import Vault from 'hashi-vault-js';
import { promises as fs } from 'fs';

export class PrivateKeyRepository {
    private _vaultClient: Vault;
    private pkKey: string = 'private_key';
    private getTokenAsync!: () => Promise<string>;

    constructor() {
        this._vaultClient = new Vault( {
            baseUrl: process.env.TrainSolver__HashcorpKeyVaultUri,
            rootPath: process.env.TrainSolver__HashcorpKeyVaultMountPath,
            timeout: 2000,
            proxy: false,            
        });

        this.initLogin();
    }   

    public async getAsync(address: string): Promise<string> {
        const token = await this.getTokenAsync();
        const {data} = await this._vaultClient.readKVSecret(
            token, 
            address);

        return data[this.pkKey];
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

                const { client_token } = await this._vaultClient.loginWithK8s(k8sRole, k8sJWT);
                return client_token;
            }
            : async () => {
                const { client_token } = await this._vaultClient.loginWithUserpass(
                    process.env.TrainSolver__HashicorpKeyVaultUsername, 
                    process.env.TrainSolver__HashicorpKeyVaultPassword);
                return client_token;
            };
        }
}