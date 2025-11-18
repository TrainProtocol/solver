using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaComposeTransactionRequest
{
    public required DetailedNetworkDto Network { get; set; } = null!;

    public required string FromAddress { get; set; } = null!;

    public required string CallData { get; set; } = null!;
}
