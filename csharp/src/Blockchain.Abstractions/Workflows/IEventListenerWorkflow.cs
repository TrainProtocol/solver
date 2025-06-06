﻿using Temporalio.Workflows;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

[Workflow]
public interface IEventListenerWorkflow
{
    [WorkflowQuery]
    ulong? GetLastScannedBlock();

    [WorkflowQuery]
    HashSet<string> ProcessedTransactionHashes();

    [WorkflowRun]
    Task RunAsync(string networkName, NetworkType networkType, uint blockBatchSize, int waitInterval, ulong? lastProcessedBlockNumber = null);
}
