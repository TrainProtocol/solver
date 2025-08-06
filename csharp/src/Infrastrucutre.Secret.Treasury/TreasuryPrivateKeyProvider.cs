using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastrucutre.Secret.Treasury.Client;
using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastrucutre.Secret.Treasury;

public class TreasuryPrivateKeyProvider() : IPrivateKeyProvider
{
    public async Task<string> GenerateAsync(string signerAgentUrl, NetworkType type)
    {
        var generateResponse = await TreasuryClientFactory
            .Create(signerAgentUrl)
            .GenerateAddressAsync(type.ToString());

        if (!generateResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to generate address. Error:{generateResponse.Error?.Content}");
        }

        return generateResponse.Content!.Address;
    }

    public async Task<string> SignAsync(
        string signerAgentUrl,
        NetworkType type, 
        string publicKey, 
        string message)
    {
        var signedTransactionResponse = await TreasuryClientFactory
            .Create(signerAgentUrl)
            .SignTransactionAsync(
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
