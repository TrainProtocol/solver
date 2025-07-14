using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.EVM.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Blockchain.EVM.Models;
using static Temporalio.Workflows.Workflow;
using System.Numerics;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Common.Extensions;

namespace Train.Solver.Blockchain.EVM.Workflows;

[Workflow]
public class EVMTransactionProcessor : ITransactionProcessor
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
            (IEVMBlockchainActivities x) => x.BuildTransactionAsync(
                new TransactionBuilderRequest()
                {
                    Network = request.Network,
                    Args = request.PrepareArgs,
                    Type = request.Type
                }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));

        // Estimate fee
        if (context.Fee == null)
        {
            context.Fee = await GetFeeAsync(request, context, preparedTransaction);
        }

        // Get nonce
        if (string.IsNullOrEmpty(context.Nonce))
        {
            context.Nonce = await ExecuteActivityAsync(
                (IEVMBlockchainActivities x) => x.GetNextNonceAsync(new()
                {
                    Network = request.Network,
                    Address = request.FromAddress!,
                }),
                TemporalHelper.DefaultActivityOptions(request.Network.Type));
        }

        var rawTransaction = await ExecuteActivityAsync(
            (IEVMBlockchainActivities x) => x.ComposeSignedRawTransactionAsync(new EVMComposeTransactionRequest()
            {
                Network = request.Network,
                FromAddress = request.FromAddress,
                ToAddress = preparedTransaction.ToAddress,
                Nonce = context.Nonce,
                AmountInWei = preparedTransaction.AmountInWei,
                CallData = preparedTransaction.Data,
                Fee = context.Fee
            }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));

        // Initiate blockchain transfer
        try
        {
            var txId = await ExecuteActivityAsync(
                (IEVMBlockchainActivities x) => x.PublishRawTransactionAsync(
                    new EVMPublishTransactionRequest()
                    {
                        Network = request.Network,
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
                        Network = request.Network,
                    }),
                    TemporalHelper.DefaultActivityOptions(request.Network.Type));

                context.Fee = increasedFee;
                context.Attempts++;

                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.RunAsync(request, context));
            }

            throw;
        }

        var confirmedTransaction = await GetTransactionReceiptAsync(request, context);

        confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
        confirmedTransaction.Amount = preparedTransaction.CallDataAmountInWei;

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
                (IEVMBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
                {
                    Network = request.Network,
                    FromAddress = request.FromAddress!,
                    ToAddress = preparedTransaction.ToAddress!,
                    Asset = preparedTransaction.Asset!,
                    Amount = preparedTransaction.AmountInWei,
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
                    await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((x) => x.RunAsync(
                        new TransactionRequest()
                        {
                            Network = request.Network,
                            FromAddress = request.FromAddress,
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
                        new() { Id = TemporalHelper.BuildProcessorId(request.Network.Name, TransactionType.Transfer, NewGuid()) });
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

        // Check allowance
        var allowance = await ExecuteActivityAsync(
            (IEVMBlockchainActivities x) => x.GetSpenderAllowanceAsync(new AllowanceRequest()
            {
                Network = context.Network,
                OwnerAddress = context.FromAddress,
                Asset = lockRequest.SourceAsset
            }),
            TemporalHelper.DefaultActivityOptions(context.Network.Type));

        if (BigInteger.Parse(lockRequest.Amount) > BigInteger.Parse(allowance))
        {
            // Initiate approval transaction

            await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((x) => x.RunAsync(new TransactionRequest()
            {
                PrepareArgs = JsonSerializer.Serialize(new ApprovePrepareRequest
                {
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }, (JsonSerializerOptions?)null),
                Type = TransactionType.Approve,
                FromAddress = context.FromAddress,
                Network = context.Network,
                SwapId = context.SwapId,
            },
            new()), new() { Id = TemporalHelper.BuildProcessorId(context.Network.Name, TransactionType.Approve, NewGuid()) });

        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
               (IEVMBlockchainActivities x) => x.GetBatchTransactionAsync(new GetBatchTransactionRequest()
               {
                   Network = request.Network,
                   TransactionHashes = context.PublishedTransactionIds.ToArray()
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
