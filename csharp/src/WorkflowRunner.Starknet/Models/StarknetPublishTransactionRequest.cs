﻿using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.WorkflowRunner.Starknet.Models;

public class StarknetPublishTransactionRequest : BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public string CallData { get; set; } = null!;

    public string Nonce { get; set; } = null!;

    public Fee Fee { get; set; } = null!;
}
