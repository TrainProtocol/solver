namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaAddLockSigMessage
{
    public string Id { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public string SignerAddress { get; set; } = null!;

    public long Timelock { get; set; }
}
