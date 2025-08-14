using Temporalio.Workflows;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using static Temporalio.Workflows.Workflow;
using static Train.Solver.Workflow.Common.Helpers.TemporalHelper;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class RefundWorkflow : IScheduledWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var swaps = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetNonRefundedSwapsAsync(),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        foreach (var swap in swaps)
        {
            var network = await ExecuteActivityAsync(
                (INetworkActivities x) => x.GetNetworkAsync(swap.Destination.Network.Name),
                DefaultActivityOptions(Constants.CoreTaskQueue));

            var confirmedTransaction = await ExecuteChildTransactionProcessorWorkflowAsync(
                network.Type,
                x => x.RunAsync(new TransactionRequest()
                {
                    PrepareArgs = new HTLCRefundTransactionPrepareRequest
                    {
                        CommitId = swap.CommitId,
                        Asset = swap.Destination.Token.Symbol,
                    }.ToJson(),
                    Type = TransactionType.HTLCRefund,
                    Network = network,
                    FromAddress = swap.DestinationWallet.Address,
                    SignerAgentUrl = swap.DestinationWallet.SignerAgent.Url,
                    SwapId = swap.Id,
                }, new TransactionExecutionContext()));

            await ExecuteActivityAsync(
                (ISwapActivities x) =>
                    x.CreateSwapTransactionAsync(swap.Id, TransactionType.HTLCRefund, confirmedTransaction),
                DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}