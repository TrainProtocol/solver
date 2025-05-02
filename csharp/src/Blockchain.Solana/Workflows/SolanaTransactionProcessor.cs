using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Common.Extensions;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Solana.Activities;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Blockchain.Solana.Models;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Solana.Workflows;

[Workflow]
public class SolanaTransactionProcessor
{
    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest 
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
                (ISolanaBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
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
            (ISolanaBlockchainActivities x) => x.GetNextNonceAsync(
                new NextNonceRequest()
                {
                    Address = request.NetworkName,
                    NetworkName = request.NetworkName,
                }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        var rawTx = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.ComposeSolanaTranscationAsync(new SolanaComposeTransactionRequest()
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
                (ISolanaBlockchainActivities x) => x.SimulateTransactionAsync(
                    new SolanaPublishTransactionRequest()
                        {
                            RawTx = rawTx,
                            NetworkName = request.NetworkName
                        }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            //Send transaction

            var transactionId = await ExecuteActivityAsync(
                (ISolanaBlockchainActivities x) => x.PublishTransactionAsync(
                    new SolanaPublishTransactionRequest()
                    {
                        RawTx = rawTx,
                        NetworkName = request.NetworkName
                    }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (ISolanaBlockchainActivities x) => x.GetTransactionAsync(
                    new GetTransactionRequest()
                    {
                        NetworkName = request.NetworkName,
                        TransactionHash = transactionId
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
