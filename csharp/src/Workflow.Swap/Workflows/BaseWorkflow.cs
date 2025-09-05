using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using static Train.Solver.Workflow.Common.Helpers.TemporalHelper;
using static Temporalio.Workflows.Workflow;
using Train.Solver.Workflow.Common;

namespace Train.Solver.Workflow.Swap.Workflows;

public abstract class BaseWorkflow
{
    public virtual async Task<TransactionResponse> ExecuteTransactionAsync(TransactionRequest transactionRequest)
    {
        var confirmedTransaction = await ExecuteChildTransactionProcessorWorkflowAsync(
            transactionRequest.Network.Type,
            x => x.RunAsync(transactionRequest, new TransactionExecutionContext()),
            new ChildWorkflowOptions
            {
                Id = BuildProcessorId(
                    transactionRequest.Network.Name,
                    transactionRequest.Type,
                    NewGuid()),
                TaskQueue = transactionRequest.Network.Type.ToString(),
            });

        await ExecuteActivityAsync(
            (ISwapActivities x) =>
                x.CreateSwapTransactionAsync(transactionRequest.SwapId, transactionRequest.Type, confirmedTransaction),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        await ExecuteActivityAsync(
            (ISwapActivities x) => x.UpdateExpensesAsync(
                confirmedTransaction.NetworkName,
                confirmedTransaction.FeeAsset,
                confirmedTransaction.FeeAmount.ToString(),
                confirmedTransaction.Asset,
                transactionRequest.Type),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        return confirmedTransaction;
    }
}
