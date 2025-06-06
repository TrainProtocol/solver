﻿using Solnet.Wallet;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

public class HTLCRedeemRequest
{
    public byte[] Id { get; set; } = null!;

    public byte[] Secret { get; set; } = null!;

    public PublicKey SourceTokenPublicKey { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public PublicKey SenderPublicKey { get; set; } = null!;

    public PublicKey RewardPublicKey { get; set; } = null!;
}
