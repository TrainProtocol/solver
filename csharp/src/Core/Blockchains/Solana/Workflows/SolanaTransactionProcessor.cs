using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.Solana.Activities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Workflows;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Blockchains.Solana.Workflows;

[Workflow]
public class SolanaTransactionProcessor : TransactionProcessorBase
{
    public static readonly ActivityOptions SolanaRetryableActivityOptions = new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromHours(1),
        StartToCloseTimeout = TimeSpan.FromDays(2),
        RetryPolicy = new()
        {
            NonRetryableErrorTypes = new[]
            {
                nameof(NonceMissMatchException),
                nameof(TransactionFailedRetriableException)
            }
        }
    };
    [WorkflowRun]
    public override Task<TransactionModel> RunAsync(TransactionContext transactionContext)
    {
        return base.RunAsync(transactionContext);
    }

    protected override async Task<TransactionModel> ExecuteTransactionAsync(TransactionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync<PrepareTransactionResponse>(
            $"{context.NetworkGroup}{nameof(IBlockchainActivities.BuildTransactionAsync)}",
            [
                context.NetworkName,
                context.Type,
                context.PrepareArgs],
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        if (context.Fee == null)
        {
            var fee = await ExecuteActivityAsync<Fee>(
                 $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.EstimateFeeAsync)}",
               [ new EstimateFeeRequest
               {
                   FromAddress = context.FromAddress!,
                   ToAddress = preparedTransaction.ToAddress,
                   Asset = preparedTransaction.Asset,
                   Amount = preparedTransaction.Amount,
                   CallData = preparedTransaction.Data,
               }],
               TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

            if (fee is null)
            {
                throw new("Unable to pay fees");
            }

            context.Fee = fee;
        }

        if (context.Fee.Asset == preparedTransaction.CallDataAsset)
        {
            await ExecuteActivityAsync(
                 $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.EnsureSufficientBalanceAsync)}",
                 [
                   context.NetworkName,
                   context.Fee.Asset,
                   context.FromAddress,
                   context.Fee.Amount + preparedTransaction.CallDataAmount
                 ],
               TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }
        else
        {

            await ExecuteActivityAsync(
                 $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.EnsureSufficientBalanceAsync)}",
                 [
                   context.NetworkName,
                   context.FromAddress,
                   preparedTransaction.CallDataAsset,
                   preparedTransaction.CallDataAmount
                   ],
               TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        }

        var lastValidBLockHash = await ExecuteActivityAsync<string>(
            $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.GetNonceAsync)}",
             [
                context.NetworkName,
                context.FromAddress,
                context.UniquenessToken,
             ],
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        var rawTx = await ExecuteActivityAsync<byte[]>(
            $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.ComposeSolanaTranscationAsync)}",
                [context.Fee,
                context.FromAddress,
                preparedTransaction.Data!,
                lastValidBLockHash],
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        TransactionModel confirmedTransaction;

        try
        {
            //Simulate transaction
            await ExecuteActivityAsync(
                $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.SimulateTransactionAsync)}",
                    [context.NetworkName,
                    rawTx],
                TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

            //Send transaction

            var transactionId = await ExecuteActivityAsync<string>(
                $"{context.NetworkGroup}{nameof(ISolanaBlockchainActivities.PublishTransactionAsync)}",
                    [context.NetworkName,
                    rawTx],
                TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.GetSolanaTransactionReceiptAsync(
                    context.NetworkName,
                    context.FromAddress,
                    transactionId),
                TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

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
