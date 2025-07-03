using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

public class TransactionBuilderWorkflow : ITransactionBuilderWorkflow
{
    public async Task<PrepareTransactionResponse> RunAsync(TransactionBuilderRequest request)
    {
        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(request.NetworkName),
               TemporalHelper.DefaultActivityOptions());

        var buildTransaction = await ExecuteActivityAsync(
               (IBlockchainActivities x) => x.BuildTransactionAsync(request),
               TemporalHelper.DefaultActivityOptions(network.Type));

        return buildTransaction;
    }
}
