using Serilog;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Temporal.Abstractions;
using Train.Solver.Core.Temporal.Abstractions.Models;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Activities;
using Train.Solver.WorkflowRunner.Exceptions;
using Train.Solver.WorkflowRunner.Extensions;
using Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Workflows;

[Workflow]
public class StarknetTransactionProcessor : ITransactionProcessor
{
    [WorkflowRun]
    public async Task<TransactionModel> RunAsync(TransactionContext context)
    {
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        var preparedTransaction = await PrepareTransactionAsync(
                context.NetworkName,
                context.Type,
                context.PrepareArgs);

        if (context.Fee == null)
        {
            var fee= await GetFeesAsync(context, preparedTransaction);


            context.Fee = fee;
        }

        if (context.Fee.Asset == preparedTransaction.CallDataAsset)
        {
            await EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   context.Fee.Asset,
                   context.FromAddress,
                   context.Fee.Amount + preparedTransaction.CallDataAmount,
                   context.CorrelationId);
        }
        else
        {
            await EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   context.Fee.Asset,
                   context.FromAddress,
                   context.Fee.Amount,
                   context.CorrelationId);

            await EnsureSufficientBalanceAsync(
                   context.NetworkName,
                   preparedTransaction.CallDataAsset,
                   context.FromAddress,
                   preparedTransaction.CallDataAmount,
                   context.CorrelationId);
        }

        var nonce = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.GetReservedNonceAsync(
                context.NetworkName,
                context.FromAddress,
                context.UniquenessToken),
            Constants.DefaultActivityOptions);

        var txId = await InitiateBlockchainTransferAsync(
                new()
                {
                    NetworkName = context.NetworkName,
                    FromAddress = context.FromAddress!,
                    ToAddress = preparedTransaction.ToAddress!,
                    Asset = preparedTransaction.Asset!,
                    Amount = preparedTransaction.Amount,
                    ReferenceId = context.UniquenessToken,
                    CorrelationId = context.CorrelationId ?? Guid.NewGuid().ToString(),
                    Fee = context.Fee,
                    Nonce = nonce,
                    CallData = preparedTransaction.Data,
                });

        context.PublishedTransactionIds.Add(txId);

        var confirmedTransaction = await GetTransactionReceiptAsync(context);

        confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
        confirmedTransaction.Amount = preparedTransaction.CallDataAmount;

        return confirmedTransaction;
    }

    private async Task<Fee> GetFeesAsync(
        TransactionContext context,
        PrepareTransactionResponse preparedTransaction)
    {
        Fee fee;
        var feesRequest = await ExecuteActivityAsync(
            (ExternalActivities x) => x.ExternalFeeRequestAsync(
                context.NetworkName,
                new()
                {
                    FromAddress = context.FromAddress!,
                    ToAddress = preparedTransaction.ToAddress!,
                    Asset = preparedTransaction.Asset!,
                    Amount = preparedTransaction.Amount,
                    CallData = preparedTransaction.Data,
                },
                context.CorrelationId),
                Constants.DefaultActivityOptions);

        try
        {
            var feeResponse = await ExecuteActivityAsync<Dictionary<string, Fee>>(
                "StarknetGetFeesActivity",
                new object[] { feesRequest },
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    RetryPolicy = new()
                    {
                        NonRetryableErrorTypes = new[]
                        {
                            typeof(InvalidTimelockException).Name
                        }
                    }
                });

            if (!feeResponse.Any())
            {
                throw new("Unable to pay fees");
            }

            fee = feeResponse.First().Value;
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx && appFailEx.HasError<InvalidTimelockException>())
            {
                if (!string.IsNullOrEmpty(context.Nonce))
                {
                    await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((x) => x.RunAsync(new()
                    {
                        CorrelationId = context.CorrelationId,
                        UniquenessToken = context.UniquenessToken,
                        NetworkName = context.NetworkName,
                        Nonce = context.Nonce,
                        FromAddress = context.FromAddress,
                        PrepareArgs = new TransferPrepareRequest
                        {
                            Amount = 0,
                            Asset = context.Fee!.Asset,
                            ToAddress = context.FromAddress,
                        }.ToArgs(),
                        Type = TransactionType.Transfer,
                    }));
                }

                throw;
            }
            // if lock already exists
            else if (ex.InnerException is ApplicationFailureException appFailException && appFailException.HasError<HashlockAlreadySetException>())
            {
                if (context.Fee == null)
                {
                    Log.Error("Hashlock already set, first attempt");
                    throw;
                }

                return context.Fee;
            }
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<HTLCAlreadyExistsException>())
            {
                var confirmedTransaction = await GetTransactionReceiptAsync(context);

                if (confirmedTransaction != null)
                {
                    return context.Fee!;
                }

                throw;
            }

            throw;
        }

        return fee;
    }

    private async Task EnsureSufficientBalanceAsync(
        string networkName,
        string asset,
        string address,
        decimal amount,
        string? correlationId)
    {
        var sufficientBalanceRequest = await ExecuteActivityAsync(
            (ExternalActivities x) => x.ExternalSufficientBalanceRequestAsync(
                networkName,
                address,
                asset,
                amount,
                correlationId),
                Constants.DefaultActivityOptions);

        var balance = await ExecuteActivityAsync<Task>(
            "StarknetEnsureSufficientBalanceActivity",
            new object[] { sufficientBalanceRequest },
            Constants.DefaultJsActivityOptions);
    }

    private async Task<PrepareTransactionResponse> PrepareTransactionAsync(
        string networkName,
        TransactionType type,
        string args)
    {
        var preapareTransactionRequest = await ExecuteActivityAsync(
            (StarknetActivites x) => x.HTLCTransactionRequestBuilder(
                networkName,
                type,
                args),
            Constants.DefaultActivityOptions);

        var preparedTransactionResponse = await ExecuteActivityAsync<PrepareTransactionResponse>(
            "StarknetTransactionBuilderActivity",
            new object[] { preapareTransactionRequest },
            Constants.DefaultJsActivityOptions);

        return preparedTransactionResponse;
    }

    private async Task<string> InitiateBlockchainTransferAsync(TransferRequestMessage request)
    {
        var transferRequest = await ExecuteActivityAsync(
            (ExternalActivities x) => x.ExternalTransferRequestAsync(
                request),
                Constants.DefaultActivityOptions);

        var txId = await ExecuteActivityAsync<string>(
            "StarknetWithdrawalActivity",
            new object[] { transferRequest },
            Constants.DefaultJsActivityOptions);

        return txId;
    }

    private async Task CheckAllowanceAsync(
        TransactionContext context)
    {
        var lockRequest = context.PrepareArgs.FromArgs<HTLCLockTransactionPrepareRequest>();

        // Get spender address
        var spenderAddress = await ExecuteActivityAsync(
            (StarknetActivites x) => x.GetStarknetSpenderAddressAsync(lockRequest.SourceNetwork),
                Constants.DefaultActivityOptions);

        var allowanceRequest = await ExecuteActivityAsync(
            (ExternalActivities x) => x.ExternalAllowanceRequestAsync(
                lockRequest.SourceNetwork,
                context.FromAddress,
                spenderAddress,
                lockRequest.SourceAsset),
                Constants.DefaultActivityOptions);

        // Check allowance
        var allowance = await ExecuteActivityAsync<decimal>(
            "StarknetGetAllowanceActivity",
            new object[]
            {
                allowanceRequest
            },
            Constants.DefaultJsActivityOptions);

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction
            await ExecuteChildWorkflowAsync<StarknetTransactionProcessor>((x) => x.RunAsync(new()
            {
                PrepareArgs = new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }.ToArgs(),
                Type = TransactionType.Approve,
                CorrelationId = context.CorrelationId ?? Guid.NewGuid().ToString(),
                FromAddress = context.FromAddress,
                UniquenessToken = Guid.NewGuid().ToString(),
                NetworkName = lockRequest.SourceNetwork,
            }));
        }
    }

    private async Task<TransactionModel> GetTransactionReceiptAsync(TransactionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
                (StarknetActivites x) => x.GetStarknetTransactionReceiptAsync(
                    context.NetworkName,
                    context.PublishedTransactionIds),
                new()
                {
                    ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                    StartToCloseTimeout = TimeSpan.FromHours(1),
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(5),
                        BackoffCoefficient = 1f,
                        MaximumAttempts = 50,
                    }
                });
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx && appFailEx.HasError<TransactionNotComfirmedException>())
            {
                throw CreateContinueAsNewException<StarknetTransactionProcessor>((x) => x.RunAsync(context));
            }
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<TransactionFailedException>())
            {
                throw new ApplicationFailureException("Transaction failed");
            }

            throw;
        }
    }
}
