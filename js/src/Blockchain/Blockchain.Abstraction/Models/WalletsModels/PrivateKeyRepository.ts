import { promises as fs } from 'fs';
import nodeVault, { client } from 'node-vault'

export class PrivateKeyRepository {
    private pkKey: string = 'private_key';
    private vault: client;
    private getTokenAsync!: () => Promise<void>;

    constructor() { 
        this.vault = nodeVault({
            endpoint: process.env.TrainSolver__HashicorpKeyVaultUri
        });

        this.initLogin();
    }   

    public async getAsync(address: string): Promise<string> {
        await this.getTokenAsync();
        const keyVaultMount = process.env.TrainSolver__HashicorpKeyVaultMountPath ?? 'secret';
        const {data} = await this.vault.read(`${keyVaultMount}/data/${address}`);
        return  data.data[this.pkKey];
    }
    
    public async getStarkPKAsync(address: string, network: string): Promise<string> {
        var key = `STARK-${network.replace('_', '-')}--${address.toLowerCase()}`;
        return this.getAsync(key);
    }

    private initLogin(): void {
        const useKubernetesAuth = process.env.TrainSolver__HashicorpEnableKubernetesAuth === 'true';

        this.getTokenAsync = useKubernetesAuth
            ? async () => {
                const k8sJWTPath = process.env.K8S_SERVICE_ACCOUNT_TOKEN_PATH;
                var k8sRole = process.env.TrainSolver__HashicorpKeyVaultK8sAppRole;
                const k8sJWT = await fs.readFile(k8sJWTPath, 'utf8');

                await this.vault.kubernetesLogin({role: k8sRole , jwt: k8sJWT, mount_point: "kubernetes"});
            }
            : async () => {
                const userName = process.env.TrainSolver__HashicorpKeyVaultUsername;
                const password = process.env.TrainSolver__HashicorpKeyVaultPassword;

                await this.vault
                    .userpassLogin({
                        username: userName,
                        password: password,
                        mount_point: 'userpass',
                    })
            };
        }
}