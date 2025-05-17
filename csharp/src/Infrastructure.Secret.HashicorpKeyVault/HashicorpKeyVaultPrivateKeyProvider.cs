using Microsoft.Extensions.Options;
using Train.Solver.Infrastructure.Abstractions;
using VaultSharp;

namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public class HashicorpKeyVaultPrivateKeyProvider(
    IVaultClient secretClient,
    IOptions<HashicorpKeyVaultOptions> options) : IPrivateKeyProvider
{
    private const string _pkKey = "private_key";

    public async Task<string> GetAsync(string publicKey)
    {
        try
        {
            var secret = await secretClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: publicKey,
                    mountPoint: options.Value.HashicorpKeyVaultMountPath);

            var privateKey = secret.Data.Data[_pkKey].ToString();

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new InvalidOperationException($"Private key not found for public key: {publicKey}");
            }

            return privateKey;
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public async Task<string> SetAsync(string publicKey, string privateKey)
    {
        try
        {
            await secretClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path: publicKey,
                data: new Dictionary<string, object> { { _pkKey, privateKey } },
                mountPoint: options.Value.HashicorpKeyVaultMountPath);

            return publicKey;
        }
        catch (Exception e)
        {
            throw;
        }
    }
}
