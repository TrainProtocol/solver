using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Workflows;

public interface ISwapWorkflow
{
    [WorkflowSignal]
    Task LockCommitedAsync(HTLCLockEventMessage message);

    [WorkflowRun]
    Task RunAsync(HTLCCommitEventMessage message);

    [WorkflowUpdate]
    Task<bool> SetAddLockSigAsync(AddLockSignatureRequest addLockSig);
}