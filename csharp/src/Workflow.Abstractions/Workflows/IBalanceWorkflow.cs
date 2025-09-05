
using System.Numerics;
using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Workflows;

[Workflow]
public interface IBalanceWorkflow
{
    [WorkflowRun]
    Task<Dictionary<TokenDto, BigInteger>> RunAsync(string networkName, string address);
}