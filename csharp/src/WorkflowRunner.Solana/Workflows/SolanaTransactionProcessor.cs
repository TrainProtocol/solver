using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Abstractions.Exceptions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Extensions;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.WorkflowRunner.Solana.Activities;
using Train.Solver.WorkflowRunner.Solana.Models;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Solana.Workflows;

[Workflow]
public class SolanaTransactionProcessor
{
    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest
                {
                    NetworkName = request.NetworkName,
                    Args = request.PrepareArgs,
                    Type = request.Type
                }
            ),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        if (context.Fee == null)
        {
            var fee = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
                    {
                        NetworkName = request.NetworkName,
                        FromAddress = request.FromAddress!,
                        ToAddress = preparedTransaction.ToAddress,
                        Asset = preparedTransaction.Asset,
                        Amount = preparedTransaction.Amount,
                        CallData = preparedTransaction.Data,
                    }
                ),
               TemporalHelper.DefaultActivityOptions(request.NetworkType));

            if (fee is null)
            {
                throw new("Unable to pay fees");
            }

            context.Fee = fee;
        }

        var lastValidBLockHash = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.GetNextNonceAsync(
                new NextNonceRequest()
                {
                    Address = request.NetworkName,
                    NetworkName = request.NetworkName,
                }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        var rawTx = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.ComposeSolanaTranscationAsync(new SolanaComposeTransactionRequest()
                {
                    Fee = context.Fee,
                    FromAddress = request.FromAddress,
                    CallData = preparedTransaction.Data,
                    LastValidBlockHash = lastValidBLockHash,
                }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        TransactionResponse confirmedTransaction;

        try
        {
            //Simulate transaction
            await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.SimulateTransactionAsync(
                    new SolanaPublishTransactionRequest()
                        {
                            RawTx = rawTx,
                            NetworkName = request.NetworkName
                        }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            //Send transaction

            var transactionId = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.PublishTransactionAsync(
                    new SolanaPublishTransactionRequest()
                    {
                        RawTx = rawTx,
                        NetworkName = request.NetworkName
                    }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.GetTransactionAsync(
                    new GetTransactionRequest()
                    {
                        NetworkName = request.NetworkName,
                        TransactionId = transactionId
                    }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
            confirmedTransaction.Amount = preparedTransaction.CallDataAmount;
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx &&
               (appFailEx.HasError<NonceMissMatchException>() || appFailEx.HasError<TransactionFailedRetriableException>()))
            {
                throw CreateContinueAsNewException((SolanaTransactionProcessor x) => x.RunAsync(request, context));
            }

            throw;
        }

        return confirmedTransaction;
    }
}
