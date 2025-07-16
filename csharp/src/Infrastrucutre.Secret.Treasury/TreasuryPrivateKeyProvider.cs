using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastrucutre.Secret.Treasury.Client;
using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastrucutre.Secret.Treasury;

public class TreasuryPrivateKeyProvider(ITreasuryClient client) : IPrivateKeyProvider
{
    public async Task<string> GenerateAsync(NetworkType type, string label)
    {
        var generateResponse = await client.GenerateAddressAsync(type.ToString());

        if (!generateResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to generate address. Error:{generateResponse.Error?.Content}");
        }

        return generateResponse.Content!.Address;
    }

    public async Task<string> SignAsync(
        NetworkType type, 
        string publicKey, 
        string message)
    {
        var signedTransactionResponse = await client.SignTransactionAsync(
            type.ToString(),
            request: new()
            {
                Address = publicKey,
                UnsignedTxn = message
            });

        if (!signedTransactionResponse.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Failed to sign transaction. Error:{signedTransactionResponse.Error?.Content}");
        }

        return signedTransactionResponse.Content!.SignedTxn;
    }
}
