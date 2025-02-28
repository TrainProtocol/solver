using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Temporal.Abstractions;
using Train.Solver.Core.Temporal.Abstractions.Models;
using Train.Solver.WorkflowRunner.Activities;
using Train.Solver.WorkflowRunner.Exceptions;
using Train.Solver.WorkflowRunner.Extensions;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Solana;

[Workflow]
public class SolanaTransactionProcessor : ITransactionProcessor
{
    [WorkflowRun]
    public async Task<TransactionModel> RunAsync(TransactionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.PrepareTransactionAsync(
                context.NetworkName,
                context.Type,
                context.PrepareArgs),
            Constants.DefaultActivityOptions);

        if (context.Fee == null)
        {
            var fee = await ExecuteActivityAsync(
               (BlockchainActivities x) => x.EstimateFeesAsync(context.NetworkName, new EstimateFeeRequest
               {
                   FromAddress = context.FromAddress!,
                   ToAddress = preparedTransaction.ToAddress,
                   Asset = preparedTransaction.Asset,
                   Amount = preparedTransaction.Amount,
                   CallData = preparedTransaction.Data,
               },
               context.CorrelationId),
               Constants.DefaultActivityOptions);

            if (fee is null)
            {
                throw new("Unable to pay fees");
            }

            context.Fee = fee;
        }

        if (context.Fee.Asset == preparedTransaction.CallDataAsset)
        {
            await ExecuteActivityAsync(
               (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   context.Fee.Asset,
                   context.FromAddress,
                   context.Fee.Amount + preparedTransaction.CallDataAmount),
               Constants.DefaultActivityOptions);
        }
        else
        {
            await ExecuteActivityAsync(
               (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   context.Fee.Asset,
                   context.FromAddress,
                   context.Fee.Amount),
               Constants.DefaultActivityOptions);

            await ExecuteActivityAsync(
               (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   preparedTransaction.CallDataAsset,
                   context.FromAddress,
                   preparedTransaction.CallDataAmount),
               Constants.DefaultActivityOptions);
        }

        var lastValidBLockHash = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.GetReservedNonceAsync(
                context.NetworkName,
                context.FromAddress,
                context.UniquenessToken),
            Constants.DefaultActivityOptions);

        var rawTx = await ExecuteActivityAsync(
            (SolanaActivities x) => x.ComposeSolanaTranscationAsync(
                context.Fee,
                context.FromAddress,
                preparedTransaction.Data!,
                lastValidBLockHash),
            Constants.DefaultActivityOptions);

        TransactionModel confirmedTransaction;

        try
        {
            //Simulate transaction
            await ExecuteActivityAsync(
                (SolanaActivities x) => x.IsValidSolanaTransactionAsync(
                    context.NetworkName,
                    rawTx),
                Constants.SolanaRetryableActivityOptions);

            //Send transaction

            var transactionId = await ExecuteActivityAsync(
                (SolanaActivities x) => x.SolanaSendTransactionAsync(
                    context.NetworkName,
                    rawTx),
                Constants.SolanaRetryableActivityOptions);

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (SolanaActivities x) => x.GetSolanaTransactionReceiptAsync(
                    context.NetworkName,
                    context.FromAddress,
                    transactionId),
                Constants.SolanaRetryableActivityOptions);

            confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
            confirmedTransaction.Amount = preparedTransaction.CallDataAmount;
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx &&
               (appFailEx.HasError<NonceMissMatchException>() || appFailEx.HasError<TransactionFailedRetriableException>()))
            {
                throw CreateContinueAsNewException((SolanaTransactionProcessor x) => x.RunAsync(context));
            }

            throw;
        }

        return confirmedTransaction;
    }
}
