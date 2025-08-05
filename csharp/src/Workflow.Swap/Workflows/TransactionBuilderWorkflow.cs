using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Common.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class TransactionBuilderWorkflow : ITransactionBuilderWorkflow
{
    [WorkflowRun]
    public async Task<PrepareTransactionDto> RunAsync(PrepareTransactionRequest request)
    {
        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(request.NetworkName),
               TemporalHelper.DefaultActivityOptions());

        var buildTransaction = await ExecuteActivityAsync(
               (ITransactionBuilderActivities x) => x.BuildTransactionAsync(new()
               {
                     Type = request.Type,
                     Args = request.Args,
                     Network = network
               }),
               TemporalHelper.DefaultActivityOptions(network.Type));

        return buildTransaction;
    }
}
