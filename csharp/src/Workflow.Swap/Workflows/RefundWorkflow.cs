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
    public async Task RunAsync(string commitId, string networkName, string fromAddress, string signerAgentName)
    {
        var swap = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetSwapAsync(commitId),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(networkName),
               DefaultActivityOptions(Constants.CoreTaskQueue));

        var asset = swap.Destination.Network.Name == network.Name
            ? swap.Destination.Token.Symbol
            : swap.Source.Token.Symbol;

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
                    Asset = asset,
                }.ToJson(),
                Type = TransactionType.HTLCRefund,
                Network = network,
                FromAddress = fromAddress,
                SignerAgentUrl = signerAgent.Url,
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