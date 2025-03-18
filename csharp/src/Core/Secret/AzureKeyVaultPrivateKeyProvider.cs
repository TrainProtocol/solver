using Azure.Security.KeyVault.Secrets;
using FluentResults;
using Serilog;

namespace Train.Solver.Core.Secret;

public class AzureKeyVaultPrivateKeyProvider(
    SecretClient secretClient) : IPrivateKeyProvider
{

    public async Task<Result<string>> GetAsync(string publicKey)
    {
        try
        {
            var privateKey = (await secretClient.GetSecretAsync(publicKey)).Value.Value;

            return Result.Ok(privateKey);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception while getting private key for address: {publicKey}");
            return Result.Fail($"Couldn't get secret for address {publicKey}.");
        }
    }

    public async Task<Result<string>> SetAsync(string publicKey, string privateKey)
    {
        try
        {
            await secretClient.SetSecretAsync(publicKey, privateKey);

            return Result.Ok(publicKey);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception while settings private key for address: {publicKey}");
            return Result.Fail($"Couldn't set secret for address {publicKey}.");
        }

    }
}
