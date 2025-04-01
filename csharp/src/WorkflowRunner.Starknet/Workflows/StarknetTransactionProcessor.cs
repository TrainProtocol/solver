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
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        if (request.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(request);
        }

        var preparedTransaction = await ExecuteActivityAsync(
            (StarknetBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest()
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
                    (StarknetBlockchainActivities x) => x.GetNextNonceAsync(new NextNonceRequest()
                    {
                        NetworkName = request.NetworkName,
                        Address = request.FromAddress!,
                    }),
                    TemporalHelper.DefaultActivityOptions(request.NetworkType));
            }

            var calculatedTxId = await ExecuteActivityAsync<string>(
                $"{request.NetworkType}{nameof(IStarknetBlockchainActivities.SimulateTransactionAsync)}",
                [
                    new StarknetPublishTransactionRequest()
                    {
                        NetworkName = request.NetworkName,
                        FromAddress = request.FromAddress,
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
                    await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((StarknetTransactionProcessor x) => x.RunAsync(
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
        return ExecuteActivityAsync<Fee>(
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
            await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((StarknetTransactionProcessor x) => x.RunAsync(new TransactionRequest()
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
            }, new()), new() { Id = TemporalHelper.BuildProcessorId(context.NetworkName, TransactionType.Approve, NewGuid()) });
        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
            (StarknetBlockchainActivities x) => x.GetBatchTransactionAsync(
                new GetBatchTransactionRequest()
                {
                    NetworkName = request.NetworkName,
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
