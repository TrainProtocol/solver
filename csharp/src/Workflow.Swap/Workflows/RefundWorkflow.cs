using Temporalio.Workflows;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using static Temporalio.Workflows.Workflow;
using static Train.Solver.Workflow.Common.Helpers.TemporalHelper;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class RefundWorkflow : IRefundWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(string commitId, string fromAddress, string signerAgentName)
    {
        var swap = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetSwapAsync(commitId),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(swap.Destination.Network.Name),
               DefaultActivityOptions(Constants.CoreTaskQueue));

        var signerAgent = await ExecuteActivityAsync(
            (IWalletActivities x) => x.GetSignerAgentAsync(signerAgentName),
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
                FromAddress = fromAddress,
                SignerAgentUrl = signerAgent.Url,
                SwapId = swap.Id,
            }, new TransactionExecutionContext()), new ChildWorkflowOptions
            {
                Id = BuildProcessorId(
                    network.Name,
                    TransactionType.HTLCRefund,
                    NewGuid()),
                TaskQueue = network.Type.ToString(),
            });

        await ExecuteActivityAsync(
            (ISwapActivities x) =>
                x.CreateSwapTransactionAsync(swap.Id, TransactionType.HTLCRefund, confirmedTransaction),
            DefaultActivityOptions(Constants.CoreTaskQueue));
    }
}