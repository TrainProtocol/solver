using Temporalio.Workflows;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

[Workflow]
public interface IEventListenerWorkflow
{
    [WorkflowQuery]
    ulong? GetLastScannedBlock();

    [WorkflowQuery]
    HashSet<string> ProcessedTransactionHashes();

    [WorkflowRun]
    Task RunAsync(string networkName, uint blockBatchSize, int waitInterval, ulong? lastProcessedBlockNumber = null);
}
