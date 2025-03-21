using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchains.Starknet.Activities;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Workflows;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Blockchains.Starknet.Workflows;

[Workflow]
public class StarknetTransactionProcessor : TransactionProcessorBase
{
    [WorkflowRun]
    public override Task<TransactionModel> RunAsync(TransactionContext transactionContext)
    {
        return base.RunAsync(transactionContext);
    }

    protected override async Task<TransactionModel> ExecuteTransactionAsync(TransactionContext context)
    {
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        var preparedTransaction = await ExecuteActivityAsync<PrepareTransactionResponse>(
            $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.BuildTransactionAsync)}",
            [context.NetworkName, context.Type, context.PrepareArgs],
            TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

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
                context.Nonce = await ExecuteActivityAsync<string>(
                    $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.GetReservedNonceAsync)}",
                    [context.NetworkName, context.FromAddress!, context.UniquenessToken],
                    TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
            }

            var calculatedTxId = await ExecuteActivityAsync<string>(
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.SimulateTransactionAsync)}",
                [
                    context.FromAddress,
                    context.NetworkName,
                    context.Nonce,
                    preparedTransaction.Data,
                    context.Fee
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = Constants.JsTaskQueue,
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

            var txId = await ExecuteActivityAsync<string>(
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.PublishTransactionAsync)}",
                [
                    context.FromAddress,
                    context.NetworkName,
                    context.Nonce,
                    preparedTransaction.Data,
                    context.Fee
                ],
                TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

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
                    await ExecuteChildWorkflowAsync(nameof(StarknetTransactionProcessor), [new TransactionContext()
                    {
                        UniquenessToken = context.UniquenessToken,
                        NetworkName = context.NetworkName,
                        Nonce = context.Nonce,
                        FromAddress = context.FromAddress,
                        NetworkGroup = context.NetworkGroup,
                        PrepareArgs = JsonSerializer.Serialize(new TransferPrepareRequest
                        {
                            Amount = 0,
                            Asset = context.Fee!.Asset,
                            ToAddress = context.FromAddress,
                        }),
                        Type = TransactionType.Transfer,
                        SwapId = context.SwapId,
                    }], new() { Id = TransactionProcessorBase.BuildId(context.NetworkName, TransactionType.Transfer) });
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
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.EstimateFeeAsync)}",
                [
                    context.NetworkName,
                    new EstimateFeeRequest
                    {
                        FromAddress = context.FromAddress!,
                        ToAddress = preparedTransaction.ToAddress!,
                        Asset = preparedTransaction.Asset!,
                        Amount = preparedTransaction.Amount,
                        CallData = preparedTransaction.Data,
                    }
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = Constants.JsTaskQueue,
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
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                    context.NetworkName,
                    context.FromAddress!,
                    fee.Asset!,
                    preparedTransaction.CallDataAmount + fee.Amount
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = Constants.JsTaskQueue,
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
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                    context.NetworkName,
                    context.FromAddress!,
                    fee.Asset,
                    fee.Amount
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = Constants.JsTaskQueue,
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromMinutes(10),
                        BackoffCoefficient = 1f,
                    },
                });

            // Transfeable asset ensure balance
            await ExecuteActivityAsync(
                $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.EnsureSufficientBalanceAsync)}",
                [
                     context.NetworkName,
                    context.FromAddress!,
                    preparedTransaction.CallDataAsset,
                    preparedTransaction.CallDataAmount
                ],
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    TaskQueue = Constants.JsTaskQueue,
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
        var spenderAddress = await ExecuteActivityAsync<string>(
             $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.GetSpenderAddressAsync)}",
             [lockRequest.SourceNetwork, lockRequest.SourceAsset],
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        // Check allowance
        var allowance = await ExecuteActivityAsync<decimal>(
            $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.GetSpenderAllowanceAsync)}",
                [
                    lockRequest.SourceNetwork,
                    context.FromAddress,
                    spenderAddress,
                    lockRequest.SourceAsset
                ],
                TemporalHelper.DefaultActivityOptions(context.NetworkGroup));

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction
            await ExecuteChildWorkflowAsync(nameof(StarknetTransactionProcessor), [new TransactionContext()
            {
                PrepareArgs = JsonSerializer.Serialize(new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }),
                Type = TransactionType.Approve,
                UniquenessToken = Guid.NewGuid().ToString(),
                FromAddress = context.FromAddress,
                NetworkName = lockRequest.SourceNetwork,
                NetworkGroup = context.NetworkGroup,
                SwapId = context.SwapId,
            }], new() { Id = TransactionProcessorBase.BuildId(context.NetworkName, TransactionType.Approve) });
        }
    }

    private async Task<TransactionModel> GetTransactionReceiptAsync(TransactionContext context)
    {
        try
        {
            return await ExecuteActivityAsync<TransactionModel>(
            $"{context.NetworkGroup}{nameof(IStarknetBlockchainActivities.GetBatchTransactionAsync)}",
            [
                context.NetworkName,
                context.PublishedTransactionIds
            ],
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
