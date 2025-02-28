using Temporalio.Workflows;
using Train.Solver.Core.Temporal.Abstractions.Models;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class TransactionWorkflow
{
    [WorkflowRun]
    public virtual async Task<TransactionModel> ExecuteTransactionAsync(
        TransactionContext transactionContext,
        string? swapId = null)
    {
        var transactionProcessor = await ExecuteActivityAsync(
            (SwapActivities x) => x.SelectTransactionProcessorAsync(
                transactionContext.NetworkName), Constants.DefaultActivityOptions);

        var swapReferenceTransactionId = await ExecuteActivityAsync(
            (SwapActivities x) => x.CreateSwapReferenceTransactionAsync(
            transactionContext.NetworkName,
            swapId,
            transactionContext.Type), Constants.DefaultActivityOptions);

        transactionContext.UniquenessToken = swapReferenceTransactionId.ToString();

        var confirmedTransaction = await ExecuteChildWorkflowAsync<TransactionModel>(
            transactionProcessor, [transactionContext], new ChildWorkflowOptions
            {
                Id = $"{transactionProcessor}-{transactionContext.NetworkName}-{transactionContext.Type}-{swapReferenceTransactionId}",
            });

        await ExecuteActivityAsync(
            (SwapActivities x) =>
                x.UpdateSwapReferenceTransactionAsync(swapReferenceTransactionId, confirmedTransaction),
            Constants.DefaultActivityOptions);

        await ExecuteActivityAsync(
            (SwapActivities x) => x.UpdateTransactionComplitionDetailsAsync(
                confirmedTransaction.NetworkName,
                confirmedTransaction.FeeAsset!,
                confirmedTransaction.FeeAmount.GetValueOrDefault(),
                confirmedTransaction.Asset,
                transactionContext.Type),
            Constants.DefaultActivityOptions);
        return confirmedTransaction;
    }

    public static string BuildId(string networkName, TransactionType type) => $"{nameof(TransactionWorkflow)}-{networkName}-{type}-{Guid.NewGuid()}";
}
