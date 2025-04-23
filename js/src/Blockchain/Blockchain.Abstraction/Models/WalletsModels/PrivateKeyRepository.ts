import Vault from 'hashi-vault-js';

export class PrivateKeyRepository {
    private _vaultClient: Vault; // TODO: use k8s auth
    private pkKey: string = 'private_key';

    constructor() {
        this._vaultClient = new Vault( {
            baseUrl: process.env.TrainSolver__HashcorpKeyVaultUri,
            rootPath: process.env.TrainSolver__HashcorpKeyVaultMountPath,
            timeout: 2000,
            proxy: false,            
        });
    }

    public async getAsync(address: string): Promise<string> {
        const {data} = await this._vaultClient.readKVSecret(
            process.env.TrainSolver__HashcorpKeyVaultToken, 
            address);

        return data[this.pkKey];
    }

    public async setAsync(address: string, privateKey: string): Promise<string> {
        await this._vaultClient.createKVSecret(
            process.env.TrainSolver__HashcorpKeyVaultToken,
            this.pkKey,
            address);

        return privateKey;
    }

    public async getStarkPKAsync(address: string, network: string): Promise<string> {
        var key = `STARK-${network.replace('_', '-')}--${address.toLowerCase()}`;
        return this.getAsync(key);
    }
}