using Azure.Security.KeyVault.Secrets;
using Serilog;
using Train.Solver.Core.Services;

namespace Train.Solver.Secret.AzureKeyVault;

public class AzureKeyVaultPrivateKeyProvider(
    SecretClient secretClient) : IPrivateKeyProvider
{
    public async Task<string> GetAsync(string publicKey)
    {
        try
        {
            var privateKey = (await secretClient.GetSecretAsync(publicKey)).Value.Value;

            return privateKey;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception while getting private key for address: {publicKey}");
            throw;
        }
    }

    public async Task<string> SetAsync(string publicKey, string privateKey)
    {
        try
        {
            await secretClient.SetSecretAsync(publicKey, privateKey);

            return publicKey;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception while settings private key for address: {publicKey}");
            throw;
        }

    }
}
