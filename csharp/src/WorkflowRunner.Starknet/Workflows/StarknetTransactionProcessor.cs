using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Exceptions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Extensions;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.WorkflowRunner.Starknet.Activities;
using Train.Solver.WorkflowRunner.Starknet.Models;
using static Temporalio.Workflows.Workflow;
using TransactionResponse = Train.Solver.Core.Abstractions.Models.TransactionResponse;

namespace Train.Solver.WorkflowRunner.Starknet.Workflows;

[Workflow]
public class StarknetTransactionProcessor
{
    private const string JS_TASK_QUEUE = $"{nameof(NetworkType.Starknet)}JS";

    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionContext context)
    {
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        var preparedTransaction = await ExecuteActivityAsync(
            (StarknetBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest()
            {
                NetworkName = context.NetworkName,
                Args = context.PrepareArgs,
                Type = context.Type
            }),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        try
        {
            if (context.Fee == null)
            {
                var fee = await GetFeesAsync(context, preparedTransaction);

                context.Fee = fee;
            }

            // Get nonce
            if (string.IsNullOrEmpty(context.Nonce))
            {
                context.Nonce = await ExecuteActivityAsync(
                    (StarknetBlockchainActivities x) => x.GetReservedNonceAsync(new ReservedNonceRequest()
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        ReferenceId = context.UniquenessToken
                    }),
                    TemporalHelper.DefaultActivityOptions(context.NetworkType));
            }

            var calculatedTxId = await ExecuteActivityAsync<string>(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.SimulateTransactionAsync)}",
                [
                    new StarknetPublishTransactionRequest()
                    {
                        NetworkName = context.NetworkName,
                        FromAddress = context.FromAddress,
                        Nonce = context.Nonce,
                        CallData = preparedTransaction.Data,
                        Fee = context.Fee
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
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

            context.PublishedTransactionIds.Add(calculatedTxId);

            var txId = await ExecuteActivityAsync(
                (StarknetBlockchainActivities x) => x.PublishTransactionAsync(new StarknetPublishTransactionRequest()
                {
                    NetworkName = context.NetworkName,
                    FromAddress = context.FromAddress,
                    Nonce = context.Nonce,
                    CallData = preparedTransaction.Data,
                    Fee = context.Fee
                }
                ),
                TemporalHelper.DefaultActivityOptions(context.NetworkType));

            context.PublishedTransactionIds.Add(txId);

            var confirmedTransaction = await GetTransactionReceiptAsync(context);

            confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
            confirmedTransaction.Amount = preparedTransaction.CallDataAmount;

            return confirmedTransaction;
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx && appFailEx.HasError<InvalidTimelockException>())
            {
                if (!string.IsNullOrEmpty(context.Nonce))
                {
                    await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((StarknetTransactionProcessor x) => x.RunAsync(new TransactionContext()
                    {
                        UniquenessToken = context.UniquenessToken,
                        NetworkName = context.NetworkName,
                        Nonce = context.Nonce,
                        FromAddress = context.FromAddress,
                        NetworkType = context.NetworkType,
                        PrepareArgs = JsonSerializer.Serialize(new TransferPrepareRequest
                        {
                            Amount = 0,
                            Asset = context.Fee!.Asset,
                            ToAddress = context.FromAddress,
                        }, (JsonSerializerOptions?)null),
                        Type = TransactionType.Transfer,
                        SwapId = context.SwapId,
                    }), new() { Id = TemporalHelper.BuildProcessorId(context.NetworkName, TransactionType.Transfer, NewGuid()) });
                }
            }

            throw;
        }
    }

    private async Task<Fee> GetFeesAsync(
        TransactionContext context,
        PrepareTransactionResponse preparedTransaction)
    {
        var fee = await ExecuteActivityAsync<Fee>(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.EstimateFeeAsync)}",
                [
                    new EstimateFeeRequest
                    {
                        FromAddress = context.FromAddress!,
                        ToAddress = preparedTransaction.ToAddress!,
                        Asset = preparedTransaction.Asset!,
                        Amount = preparedTransaction.Amount,
                        CallData = preparedTransaction.Data,
                        NetworkName = context.NetworkName,
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
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

        if (fee.Asset == preparedTransaction.CallDataAsset)
        {
            await ExecuteActivityAsync(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                    new SufficientBalanceRequest
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        Asset = fee.Asset!,
                        Amount = fee.Amount + preparedTransaction.CallDataAmount
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromMinutes(10),
                        BackoffCoefficient = 1f,
                    },
                });
        }
        else
        {
            // Fee asset ensure balance
            await ExecuteActivityAsync(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                    new SufficientBalanceRequest
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        Asset = fee.Asset!,
                        Amount = fee.Amount
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromMinutes(10),
                        BackoffCoefficient = 1f,
                    },
                });

            // Transfeable asset ensure balance
            await ExecuteActivityAsync(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                    new SufficientBalanceRequest
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        Asset = preparedTransaction.CallDataAsset!,
                        Amount = preparedTransaction.CallDataAmount
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromMinutes(10),
                        BackoffCoefficient = 1f,
                    },
                });
        }

        return fee;
    }

    private async Task CheckAllowanceAsync(
        TransactionContext context)
    {
        var lockRequest = JsonSerializer.Deserialize<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

        if (lockRequest is null)
        {
            throw new Exception($"Occured exception during deserializing {context.PrepareArgs}");
        }

        // Get spender address
        var spenderAddress = await ExecuteActivityAsync(
            (StarknetBlockchainActivities x) => x.GetSpenderAddressAsync(
                new SpenderAddressRequest()
                {
                    Asset = lockRequest.SourceAsset,
                    NetworkName = lockRequest.SourceNetwork,
                }
            ),
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        // Check allowance
        var allowance = await ExecuteActivityAsync<decimal>(
                $"{context.NetworkType}{nameof(IStarknetBlockchainActivities.GetSpenderAllowanceAsync)}",
                [
                    new AllowanceRequest()
                    {
                        NetworkName = lockRequest.SourceNetwork,
                        OwnerAddress = context.FromAddress,
                        SpenderAddress = spenderAddress,
                        Asset = lockRequest.SourceAsset
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = JS_TASK_QUEUE,
                });

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction
            await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((StarknetTransactionProcessor x) => x.RunAsync(new TransactionContext()
            {
                PrepareArgs = JsonSerializer.Serialize(new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }, (JsonSerializerOptions?)null),
                Type = TransactionType.Approve,
                UniquenessToken = Guid.NewGuid().ToString(),
                FromAddress = context.FromAddress,
                NetworkName = lockRequest.SourceNetwork,
                NetworkType = context.NetworkType,
                SwapId = context.SwapId,
            }), new() { Id = TemporalHelper.BuildProcessorId(context.NetworkName, TransactionType.Approve, NewGuid()) });
        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
            (StarknetBlockchainActivities x) => x.GetBatchTransactionAsync(
                new GetBatchTransactionRequest()
                {
                    NetworkName = context.NetworkName,
                    TransactionIds = context.PublishedTransactionIds.ToArray()
                }
            ),
            new()
            {
                ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                StartToCloseTimeout = TimeSpan.FromHours(1),
                RetryPolicy = new()
                {
                    InitialInterval = TimeSpan.FromSeconds(10),
                    BackoffCoefficient = 1f,
                }
            });

        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<TransactionFailedException>())
            {
                throw new ApplicationFailureException("Transaction failed");
            }

            throw;
        }
    }
}
