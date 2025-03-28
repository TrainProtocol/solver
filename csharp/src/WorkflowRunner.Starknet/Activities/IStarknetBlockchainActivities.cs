﻿using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.WorkflowRunner.Starknet.Models;

namespace Train.Solver.WorkflowRunner.Starknet.Activities;

public interface IStarknetBlockchainActivities : IBlockchainActivities
{
    Task<string> SimulateTransactionAsync(StarknetPublishTransactionRequest request);

    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);

    Task<string> PublishTransactionAsync(StarknetPublishTransactionRequest request);

    Task<Core.Abstractions.Models.TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);
}
