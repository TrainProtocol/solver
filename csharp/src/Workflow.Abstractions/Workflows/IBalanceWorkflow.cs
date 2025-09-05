
using System.Numerics;
using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Workflows;

[Workflow]
public interface IBalanceWorkflow
{
    [WorkflowRun]
    Task<NetworkBalanceDto> RunAsync(string networkName, string address);
}