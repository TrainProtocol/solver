
using Temporalio.Workflows;

namespace Train.Solver.Workflow.Abstractions.Workflows;

[Workflow]
public interface IScheduledWorkflow
{
    [WorkflowRun]
    Task RunAsync();
}