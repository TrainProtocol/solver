
using Temporalio.Workflows;

namespace Train.Solver.Workflows.Abstractions.Workflows;

[Workflow]
public interface IScheduledWorkflow
{
    [WorkflowRun]
    Task RunAsync();
}