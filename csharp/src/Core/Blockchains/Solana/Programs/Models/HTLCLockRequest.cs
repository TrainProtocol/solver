using System.Numerics;
using Solnet.Wallet;

namespace Train.Solver.Core.Blockchains.Solana.Programs.Models;

public class HTLCLockRequest
{
    public byte[] Hashlock { get; set; } = null!;

    public BigInteger Timelock { get; set; }

    public string DestinationNetwork { get; set; } = null!;

    public string SourceAddress { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public string SourceAsset { get; set; } = null!;

    public byte[] Id { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;

    public PublicKey SourceTokenPublicKey { get; set; } = null!;

    public BigInteger Amount { get; set; }

    public BigInteger Reward { get; set; }

    public BigInteger RewardTimelock { get; set; }
}
