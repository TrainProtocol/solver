﻿using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Exceptions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Extensions;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.WorkflowRunner.EVM.Activities;
using Train.Solver.WorkflowRunner.EVM.Models;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.EVM.Workflows;

[Workflow]
public class EVMTransactionProcessor
{
    const int MaxRetryCount = 5;

    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        // Check allowance
        if (request.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(request);
        }

        // Prepare transaction
        var preparedTransaction = await ExecuteActivityAsync(
            (EVMBlockchainActivities x) => x.BuildTransactionAsync(
                new TransactionBuilderRequest()
                {
                    NetworkName = request.NetworkName,
                    Args = request.PrepareArgs,
                    Type = request.Type
                }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        // Estimate fee
        if (context.Fee == null)
        {
            context.Fee = await GetFeeAsync(request, context, preparedTransaction);
        }

        // Get nonce
        if (string.IsNullOrEmpty(context.Nonce))
        {
            context.Nonce = await ExecuteActivityAsync(
                (EVMBlockchainActivities x) => x.GetNextNonceAsync(new ()
                {
                    NetworkName = request.NetworkName,
                    Address = request.FromAddress!,
                }),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));
        }

        var rawTransaction = await ExecuteActivityAsync(
            (EVMBlockchainActivities x) => x.ComposeSignedRawTransactionAsync(new EVMComposeTransactionRequest()
            {
                NetworkName = request.NetworkName,
                FromAddress = request.FromAddress,
                ToAddress = preparedTransaction.ToAddress,
                Nonce = context.Nonce,
                AmountInWei = preparedTransaction.AmountInWei,
                CallData = preparedTransaction.Data,
                Fee = context.Fee
            }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        // Initiate blockchain transfer
        try
        {
            var txId = await ExecuteActivityAsync(
                (EVMBlockchainActivities x) => x.PublishRawTransactionAsync(
                    new EVMPublishTransactionRequest()
                    {
                        NetworkName = request.NetworkName,
                        FromAddress = request.FromAddress,
                        SignedTransaction = rawTransaction
                    }),
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    RetryPolicy = new()
                    {
                        NonRetryableErrorTypes = new[]
                        {
                            typeof(TransactionUnderpricedException).Name
                        }
                    }
                });

            context.PublishedTransactionIds.Add(txId);
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx &&
                appFailEx.HasError<TransactionUnderpricedException>() &&
                context.Attempts < MaxRetryCount)
            {
                var newFee = await GetFeeAsync(request, context, preparedTransaction);

                var increasedFee = await ExecuteActivityAsync(
                    (EVMBlockchainActivities x) => x.IncreaseFeeAsync(new EVMFeeIncreaseRequest()
                    {
                        Fee = newFee,
                        NetworkName = request.NetworkName,
                    }),
                    TemporalHelper.DefaultActivityOptions(request.NetworkType));

                context.Fee = increasedFee;
                context.Attempts++;

                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.RunAsync(request, context));
            }

            throw;
        }

        var confirmedTransaction = await GetTransactionReceiptAsync(request, context);

        confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
        confirmedTransaction.Amount = preparedTransaction.CallDataAmount;

        return confirmedTransaction;
    }

    private async Task<Fee> GetFeeAsync(
        TransactionRequest request,
        TransactionExecutionContext context,
        PrepareTransactionResponse preparedTransaction)
    {
        try
        {
            var fee = await ExecuteActivityAsync(
                (EVMBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
                {
                    NetworkName = request.NetworkName,
                    FromAddress = request.FromAddress!,
                    ToAddress = preparedTransaction.ToAddress!,
                    Asset = preparedTransaction.Asset!,
                    Amount = preparedTransaction.Amount,
                    CallData = preparedTransaction.Data,
                }),
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    RetryPolicy = new()
                    {
                        NonRetryableErrorTypes = new[]
                        {
                            typeof(InvalidTimelockException).Name,
                            typeof(HashlockAlreadySetException).Name,
                            typeof(HTLCAlreadyExistsException).Name,
                            typeof(AlreadyClaimedExceptions).Name,
                        }
                    }
                });

            if (fee == null)
            {
                throw new("Unable to pay fees with any asset");
            }

            return fee;
        }
        catch (ActivityFailureException ex)
        {
            // If timelock expired
            if (ex.InnerException is ApplicationFailureException appFailEx && appFailEx.HasError<InvalidTimelockException>())
            {
                if (!string.IsNullOrEmpty(context.Nonce))
                {
                    await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((EVMTransactionProcessor x) => x.RunAsync(
                        new TransactionRequest()
                        {
                            NetworkName = request.NetworkName,
                            FromAddress = request.FromAddress,
                            NetworkType = request.NetworkType,
                            PrepareArgs = JsonSerializer.Serialize(new TransferPrepareRequest
                            {
                                Amount = 0,
                                Asset = context.Fee!.Asset,
                                ToAddress = request.FromAddress,
                            }, (JsonSerializerOptions?)null),
                            Type = TransactionType.Transfer,
                            SwapId = request.SwapId,
                        }, new TransactionExecutionContext
                        {
                            Nonce = context.Nonce,
                        }),
                        new() { Id = TemporalHelper.BuildProcessorId(request.NetworkName, TransactionType.Transfer, NewGuid()) });
                }

                throw;
            }
            //If already redeemed
            else if (ex.InnerException is ApplicationFailureException appFailException && appFailException.HasError<HashlockAlreadySetException>())
            {
                if (context.Fee == null)
                {
                    throw;
                }

                return context.Fee;
            }
            // if lock already exists
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<HTLCAlreadyExistsException>())
            {
                var confirmedTransaction = await GetTransactionReceiptAsync(request, context);
                if (confirmedTransaction != null)
                {
                    return context.Fee!;
                }
            }

            throw;
        }
    }

    private async Task CheckAllowanceAsync(
        TransactionRequest context)
    {
        var lockRequest = JsonSerializer.Deserialize<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

        if (lockRequest is null)
        {
            throw new Exception($"Occured exception during deserializing {context.PrepareArgs}");
        }

        // Get spender address
        var spenderAddress = await ExecuteActivityAsync(
            (EVMBlockchainActivities x) => x.GetSpenderAddressAsync(new SpenderAddressRequest()
            {
                Asset = lockRequest.SourceAsset,
                NetworkName = lockRequest.SourceNetwork,
            }),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        // Check allowance
        var allowance = await ExecuteActivityAsync(
            (EVMBlockchainActivities x) => x.GetSpenderAllowanceAsync(new AllowanceRequest()
            {
                NetworkName = lockRequest.SourceNetwork,
                OwnerAddress = context.FromAddress,
                SpenderAddress = spenderAddress,
                Asset = lockRequest.SourceAsset

            }),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction

            await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((EVMTransactionProcessor x) => x.RunAsync(new TransactionRequest()
            {
                PrepareArgs = JsonSerializer.Serialize(new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }, (JsonSerializerOptions?)null),
                Type = TransactionType.Approve,
                FromAddress = context.FromAddress,
                NetworkName = lockRequest.SourceNetwork,
                NetworkType = context.NetworkType,
                SwapId = context.SwapId,
            },
            new()), new() { Id = TemporalHelper.BuildProcessorId(context.NetworkName, TransactionType.Approve, NewGuid()) });

        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
               (EVMBlockchainActivities x) => x.GetBatchTransactionAsync(new GetBatchTransactionRequest()
               {
                   NetworkName = request.NetworkName,
                   TransactionIds = context.PublishedTransactionIds.ToArray()
               }),
                    new()
                    {
                        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                        StartToCloseTimeout = TimeSpan.FromHours(1),
                        RetryPolicy = new()
                        {
                            InitialInterval = TimeSpan.FromSeconds(10),
                            BackoffCoefficient = 1f,
                            MaximumAttempts = 10,
                        }
                    });
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx && appFailEx.HasError<TransactionNotComfirmedException>())
            {
                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.RunAsync(request, context));
            }
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<TransactionFailedException>())
            {
                throw new ApplicationFailureException("Transaction failed");
            }

            throw;
        }
    }
}
