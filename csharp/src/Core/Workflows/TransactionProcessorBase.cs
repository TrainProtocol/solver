using Train.Solver.Core.Activities;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Data.Entities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows;

public abstract class TransactionProcessorBase
{
    protected abstract Task<TransactionModel> ExecuteTransactionAsync(TransactionContext context);

    public virtual async Task<TransactionModel> RunAsync(TransactionContext transactionContext)
    {
        var swapReferenceTransactionId = await ExecuteActivityAsync(
            (SwapActivities x) => x.CreateSwapReferenceTransactionAsync(
            transactionContext.NetworkName,
            transactionContext.SwapId,
            transactionContext.Type), TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        transactionContext.UniquenessToken = swapReferenceTransactionId.ToString();

        var confirmedTransaction = await ExecuteTransactionAsync(transactionContext);

        await ExecuteActivityAsync(
            (SwapActivities x) =>
                x.UpdateSwapReferenceTransactionAsync(swapReferenceTransactionId, confirmedTransaction),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        await ExecuteActivityAsync(
            (SwapActivities x) => x.UpdateExpensesAsync(
                confirmedTransaction.NetworkName,
                confirmedTransaction.FeeAsset,
                confirmedTransaction.FeeAmount,
                confirmedTransaction.Asset,
                transactionContext.Type),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        return confirmedTransaction;
    }

    public static string BuildId(string networkName, TransactionType type) => $"{networkName}-{type}-{NewGuid()}";
}
