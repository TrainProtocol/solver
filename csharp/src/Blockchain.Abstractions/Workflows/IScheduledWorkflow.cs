
using Temporalio.Workflows;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

[Workflow]
public interface IScheduledWorkflow
{
    [WorkflowRun]
    Task RunAsync();
}