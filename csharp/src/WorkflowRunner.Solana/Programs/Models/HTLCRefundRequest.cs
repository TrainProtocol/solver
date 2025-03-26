﻿using Solnet.Wallet;

namespace Train.Solver.WorkflowRunner.Solana.Programs.Models;

public class HTLCRefundRequest
{
    public byte[] Id { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;

    public PublicKey SourceTokenPublicKey { get; set; } = null!;
}
