using Temporalio.Workflows;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Temporal.Abstractions;

[Workflow]
public interface ISwapWorkflow
{
    [WorkflowRun]
    Task RunAsync(HTLCCommitEventMessage message);

    [WorkflowSignal]
    Task AddLockSignatureAsync(AddLockSigRequest addlockSigMessage);

    [WorkflowSignal]
    Task LockCommitedAsync(HTLCLockEventMessage message);
}