﻿using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.EVM.Models;
public class EVMComposeTransactionRequest : BaseRequest
{
    public required string FromAddress { get; set; } = null!;

    public required string ToAddress { get; set; } = null!;

    public required string? CallData { get; set; }

    public required string Nonce { get; set; } = null!;

    public required string AmountInWei { get; set; } = null!;

    public required Fee Fee { get; set; } = null!;
}
