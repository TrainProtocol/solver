import { promises as fs } from 'fs';
import { PrivateKeyConfigService } from './vault.config';
import { HttpStatus, Inject } from '@nestjs/common';
import NodeVault = require('node-vault');
import { VaultException } from './vault.exception';


export class PrivateKeyService {
    private pkKey: string = 'private_key';
    private vault:  NodeVault.client;
    private getTokenAsync!: () => Promise<void>;

    constructor(@Inject() private privateKeyConfig: PrivateKeyConfigService) { 
        console.log(privateKeyConfig);
        this.vault = NodeVault({
            endpoint: privateKeyConfig.url
        });

        this.initLogin();
    }

    public async getAsync(address: string): Promise<string> {
        try {
            await this.getTokenAsync();
            const {data} = await this.vault.read(`${this.privateKeyConfig.mountPath}/data/${address}`);
            return  data.data[this.pkKey];
        }
         catch (error) {
            this.handleVaultError(error);
        }
    }

    public async setAsync(address: string, privateKey: string): Promise<void> {
        try {
            await this.getTokenAsync();
            await this.vault.write(`${this.privateKeyConfig.mountPath}/data/${address}`, {
                data: { [this.pkKey]: privateKey }});
        }
        catch (error) {
            this.handleVaultError(error);
        }
    }

    private initLogin(): void {
        this.getTokenAsync = 
            this.privateKeyConfig.isK8sAuthEnabled
            ? async () => {
                const k8sJWT = await fs.readFile(this.privateKeyConfig.k8sTokenPath, 'utf8');

                await this.vault.kubernetesLogin({
                    role: this.privateKeyConfig.k8sRoleName, 
                    jwt: k8sJWT, 
                    mount_point: PrivateKeyConfigService.k8sMountPoint});
            } : async () => {
                await this.vault
                    .userpassLogin({
                        username: this.privateKeyConfig.username,
                        password: this.privateKeyConfig.password,
                        mount_point: PrivateKeyConfigService.userPassMountPoint,
                    })
            };
    }

    private handleVaultError(error: any): void {
        const statusCode = error?.response?.statusCode;
        const message = {
            [HttpStatus.FORBIDDEN]: 'Failed to access Vault. Access forbidden.',
            [HttpStatus.NOT_FOUND]: 'Failed to retrieve data from Vault. Address was not found.',
        }[statusCode] ?? 'Unknown Vault error occurred';
  
        throw new VaultException(message, statusCode ?? HttpStatus.INTERNAL_SERVER_ERROR);
    }
}