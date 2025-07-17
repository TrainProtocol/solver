using Temporalio.Workflows;
using Train.Solver.Workflows.Abstractions.Models;

namespace Train.Solver.Workflows.Abstractions.Workflows;

[Workflow]
public interface ISwapWorkflow
{
    [WorkflowSignal]
    Task LockCommitedAsync(HTLCLockEventMessage message);

    [WorkflowRun]
    Task RunAsync(HTLCCommitEventMessage message);

    [WorkflowUpdate]
    Task<bool> SetAddLockSigAsync(AddLockSignatureRequest addLockSig);
}