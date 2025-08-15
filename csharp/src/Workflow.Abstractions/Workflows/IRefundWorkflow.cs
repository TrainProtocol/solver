
using Temporalio.Workflows;

namespace Train.Solver.Workflow.Abstractions.Workflows;

[Workflow]
public interface IRefundWorkflow
{
    [WorkflowRun]
    Task RunAsync(string commitId, string fromAddress, string signerAgentName);
}