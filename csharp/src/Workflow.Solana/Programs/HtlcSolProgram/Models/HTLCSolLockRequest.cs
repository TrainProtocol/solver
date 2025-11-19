using Solnet.Wallet;
using System.Numerics;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

public class HTLCSolLockRequest
{
    public required byte[] Id { get; set; } = null!;

    public required byte[] Hashlock { get; set; } = null!;

    public required BigInteger Timelock { get; set; }

    public required BigInteger Amount { get; set; }

    public required string DestinationNetwork { get; set; } = null!;

    public required string DestinationAddress { get; set; } = null!;

    public required string DestinationAsset { get; set; } = null!;

    public required string SourceAsset { get; set; } = null!;

    public required PublicKey SignerPublicKey { get; set; } = null!;

    public required PublicKey ReceiverPublicKey { get; set; } = null!;

    public required BigInteger Reward { get; set; }

    public required BigInteger RewardTimelock { get; set; }
}