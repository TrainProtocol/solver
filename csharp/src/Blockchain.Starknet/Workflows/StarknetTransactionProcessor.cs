using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Common.Extensions;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Starknet.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Blockchain.Starknet.Models;
using static Temporalio.Workflows.Workflow;
using TransactionResponse = Train.Solver.Blockchain.Abstractions.Models.TransactionResponse;

namespace Train.Solver.Blockchain.Starknet.Workflows;

[Workflow]
public class StarknetTransactionProcessor
{
    private const string JS_TASK_QUEUE = $"{nameof(NetworkType.Starknet)}JS";

    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        if (request.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(request);
        }

        var preparedTransaction = await ExecuteActivityAsync(
            (IStarknetBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest()
            {
                NetworkName = request.NetworkName,
                Args = request.PrepareArgs,
                Type = request.Type
            }),
            TemporalHelper.DefaultActivityOptions(request.NetworkType));

        try
        {
            if (context.Fee == null)
            {
                var fee = await GetFeesAsync(request, preparedTransaction);

                context.Fee = fee;
            }

            // Get nonce
            if (string.IsNullOrEmpty(context.Nonce))
            {
                context.Nonce = await ExecuteActivityAsync(
                    (IStarknetBlockchainActivities x) => x.GetNextNonceAsync(new NextNonceRequest()
                    {
                        NetworkName = request.NetworkName,
                        Address = request.FromAddress!,
                    }),
                    TemporalHelper.DefaultActivityOptions(request.NetworkType));
            }

            var calculatedTxId = await ExecuteActivityAsync((IStarknetBlockchainActivities x) => x.SimulateTransactionAsync(new StarknetPublishTransactionRequest()
            {
                NetworkName = request.NetworkName,
                FromAddress = request.FromAddress,
                Nonce = context.Nonce,
                CallData = preparedTransaction.Data,
                Fee = context.Fee
            }),
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
                (IStarknetBlockchainActivities x) => x.PublishTransactionAsync(new StarknetPublishTransactionRequest()
                {
                    NetworkName = request.NetworkName,
                    FromAddress = request.FromAddress,
                    Nonce = context.Nonce,
                    CallData = preparedTransaction.Data,
                    Fee = context.Fee
                }
                ),
                TemporalHelper.DefaultActivityOptions(request.NetworkType));

            context.PublishedTransactionIds.Add(txId);

            var confirmedTransaction = await GetTransactionReceiptAsync(request, context);

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
                    await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((x) => x.RunAsync(
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
                        },
                        new TransactionExecutionContext
                        {
                            Nonce = context.Nonce,
                        }), new() { Id = TemporalHelper.BuildProcessorId(request.NetworkName, TransactionType.Transfer, NewGuid()) });
                }
            }

            throw;
        }
    }

    private Task<Fee> GetFeesAsync(
        TransactionRequest context,
        PrepareTransactionResponse preparedTransaction)
    {
        return ExecuteActivityAsync((IStarknetBlockchainActivities x) => x.EstimateFeeAsync(new EstimateFeeRequest
        {
            FromAddress = context.FromAddress!,
            ToAddress = preparedTransaction.ToAddress!,
            Asset = preparedTransaction.Asset!,
            Amount = preparedTransaction.Amount,
            CallData = preparedTransaction.Data,
            NetworkName = context.NetworkName,
        }),
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
        var allowance = await ExecuteActivityAsync((IStarknetBlockchainActivities x) => x.GetSpenderAllowanceAsync(new AllowanceRequest()
        {
            NetworkName = lockRequest.SourceNetwork,
            OwnerAddress = context.FromAddress,
            Asset = lockRequest.SourceAsset
        }), new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromDays(2),
            StartToCloseTimeout = TimeSpan.FromHours(1),
            TaskQueue = JS_TASK_QUEUE,
        });

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction
            await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((x) => x.RunAsync(new TransactionRequest()
            {
                PrepareArgs = JsonSerializer.Serialize(new ApprovePrepareRequest
                {
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }, (JsonSerializerOptions?)null),
                Type = TransactionType.Approve,
                FromAddress = context.FromAddress,
                NetworkName = lockRequest.SourceNetwork,
                NetworkType = context.NetworkType,
                SwapId = context.SwapId,
            }, new()), new() { Id = TemporalHelper.BuildProcessorId(context.NetworkName, TransactionType.Approve, NewGuid()) });
        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
            (IStarknetBlockchainActivities x) => x.GetBatchTransactionAsync(
                new GetBatchTransactionRequest()
                {
                    NetworkName = request.NetworkName,
                    TransactionHashes = context.PublishedTransactionIds.ToArray()
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
