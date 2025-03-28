﻿using Temporalio.Exceptions;
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
    private static readonly ActivityOptions SolanaRetryableActivityOptions = new()
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
    public async Task<TransactionResponse> RunAsync(TransactionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest
                {
                    NetworkName = context.NetworkName,
                    Args = context.PrepareArgs,
                    Type = context.Type
                }
            ),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        if (context.Fee == null)
        {
            var fee = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
                    {
                        NetworkName = context.NetworkName,
                        FromAddress = context.FromAddress!,
                        ToAddress = preparedTransaction.ToAddress,
                        Asset = preparedTransaction.Asset,
                        Amount = preparedTransaction.Amount,
                        CallData = preparedTransaction.Data,
                    }
                ),
               TemporalHelper.DefaultActivityOptions(context.NetworkType));

            if (fee is null)
            {
                throw new("Unable to pay fees");
            }

            context.Fee = fee;
        }

        if (context.Fee.Asset == preparedTransaction.CallDataAsset)
        {
            await ExecuteActivityAsync(
                 (SolanaBlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                     new SufficientBalanceRequest
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        Asset = context.Fee.Asset!,
                        Amount = context.Fee.Amount + preparedTransaction.CallDataAmount
                    }
                ),
               TemporalHelper.DefaultActivityOptions(context.NetworkType));
        }
        else
        {
            await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                    new SufficientBalanceRequest
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        Asset = preparedTransaction.CallDataAsset!,
                        Amount = preparedTransaction.CallDataAmount
                    }),
               TemporalHelper.DefaultActivityOptions(context.NetworkType));

            await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                    new SufficientBalanceRequest
                     {
                         NetworkName = context.NetworkName,
                         Address = context.FromAddress!,
                         Asset = context.Fee.Asset!,
                         Amount = context.Fee.Amount
                     }),
                TemporalHelper.DefaultActivityOptions(context.NetworkType));

        }

        var lastValidBLockHash = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.GetReservedNonceAsync(
                new ReservedNonceRequest()
                {
                    Address = context.NetworkName,
                    NetworkName = context.NetworkName,
                    ReferenceId = context.UniquenessToken
                }),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        var rawTx = await ExecuteActivityAsync(
            (SolanaBlockchainActivities x) => x.ComposeSolanaTranscationAsync(new SolanaComposeTransactionRequest()
                {
                    Fee = context.Fee,
                    FromAddress = context.FromAddress,
                    CallData = preparedTransaction.Data,
                    LastValidBlockHash = lastValidBLockHash,
                }),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        TransactionResponse confirmedTransaction;

        try
        {
            //Simulate transaction
            await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.SimulateTransactionAsync(
                    new SolanaPublishTransactionRequest()
                        {
                            RawTx = rawTx,
                            NetworkName = context.NetworkName
                        }),
                TemporalHelper.DefaultActivityOptions(context.NetworkType));

            //Send transaction

            var transactionId = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.PublishTransactionAsync(
                    new SolanaPublishTransactionRequest()
                    {
                        RawTx = rawTx,
                        NetworkName = context.NetworkName
                    }),
                TemporalHelper.DefaultActivityOptions(context.NetworkType));

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (SolanaBlockchainActivities x) => x.GetTransactionAsync(
                    new GetTransactionRequest()
                    {
                        NetworkName = context.NetworkName,
                        TransactionId = transactionId
                    }),
                TemporalHelper.DefaultActivityOptions(context.NetworkType));

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
