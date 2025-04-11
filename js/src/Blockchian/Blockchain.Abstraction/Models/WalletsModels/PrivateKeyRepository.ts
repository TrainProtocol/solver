import * as ad from '@azure/identity';
import * as kv from '@azure/keyvault-secrets';

export class PrivateKeyRepository {
    private _kvClient: kv.SecretClient = new kv.SecretClient(
        process.env.TrainSolver__AzureKeyVaultUri,
        new ad.DefaultAzureCredential());

    public async getAsync(address: string): Promise<string> {
        return (await this._kvClient.getSecret(address)).value;
    }

    public async setAsync(address: string, privateKey: string): Promise<string> {
        return (await this._kvClient.setSecret(address, privateKey)).value;
    }

    public async getStarkPKAsync(address: string, network: string): Promise<string> {
        var key = `STARK-${network.replace('_', '-')}--${address.toLowerCase()}`;

        return (await this._kvClient.getSecret(key)).value;
    }
}