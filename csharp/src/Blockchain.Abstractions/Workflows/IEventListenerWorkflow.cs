using Temporalio.Workflows;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

[Workflow]
public interface IEventListenerWorkflow
{
    static abstract string BuildWorkflowId(string networkName);

    [WorkflowQuery]
    ulong? GetLastScannedBlock();

    [WorkflowQuery]
    HashSet<string> ProcessedTransactionHashes();

    [WorkflowRun]
    Task RunAsync(string networkName, NetworkType networkType, uint blockBatchSize, TimeSpan waitInterval, ulong? lastProcessedBlockNumber = null);
}