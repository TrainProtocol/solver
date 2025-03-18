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
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.EVM;

[Workflow]
public class EVMTransactionProcessor : ITransactionProcessor
{
    const int MaxRetryCount = 5;

    [WorkflowRun]
    public virtual async Task<TransactionModel> RunAsync(TransactionContext context)
    {
        // Check allowance
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        // Prepare transaction
        var preparedTransaction = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.PrepareTransactionAsync(
                context.NetworkName,
                context.Type,
                context.PrepareArgs),
            Constants.DefaultActivityOptions);

        // Estimate fee
        if (context.Fee == null)
        {
            context.Fee = await GetFeeAsync(context, preparedTransaction);
        }

        // Get nonce
        if (string.IsNullOrEmpty(context.Nonce))
        {
            context.Nonce = await ExecuteActivityAsync(
                (BlockchainActivities x) => x.GetReservedNonceAsync(
                    context.NetworkName, context.FromAddress!, context.UniquenessToken),
                Constants.DefaultActivityOptions);
        }

        var rawTransaction = await ExecuteActivityAsync(
          (EVMActivities x) => x.ComposeSignedRawTransactionAsync(
              context.NetworkName,
              context.FromAddress,
              preparedTransaction.ToAddress,
              context.Nonce,
              preparedTransaction.AmountInWei,
              preparedTransaction.Data,
              context.Fee),
          Constants.DefaultActivityOptions);

        // Initiate blockchain transfer
        try
        {
            var txId = await ExecuteActivityAsync(
                (EVMActivities x) => x.PublishRawTransactionAsync(
                    context.NetworkName,
                    context.FromAddress,
                    rawTransaction),
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
                var newFee = await GetFeeAsync(context, preparedTransaction);

                var increasedFee = await ExecuteActivityAsync(
                    (EVMActivities x) => x.IncreaseFeeAsync(
                        context.NetworkName,
                        newFee),
                    Constants.DefaultActivityOptions);

                context.Fee = increasedFee;
                context.Attempts++;

                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.RunAsync(context));
            }

            throw;
        }

        var confirmedTransaction = await GetTransactionReceiptAsync(context);

        confirmedTransaction.Asset = preparedTransaction.CallDataAsset;
        confirmedTransaction.Amount = preparedTransaction.CallDataAmount;

        return confirmedTransaction;
    }

    private async Task<Fee> GetFeeAsync(
        TransactionContext context,
        PrepareTransactionResponse preparedTransaction)
    {
        try
        {
            var fee = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.EstimateFeesAsync(
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

            if (fee.Asset == preparedTransaction.CallDataAsset)
            {
                await ExecuteActivityAsync(
                 (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                     context.NetworkName,
                     fee.Asset!,
                     context.FromAddress!,
                     preparedTransaction.CallDataAmount + fee.Amount),
                  new()
                  {
                      ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                      StartToCloseTimeout = TimeSpan.FromHours(1),
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
                 (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                     context.NetworkName,
                     fee.Asset,
                     context.FromAddress!,
                     fee.Amount),
                  new()
                  {
                      ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                      StartToCloseTimeout = TimeSpan.FromHours(1),
                      RetryPolicy = new()
                      {
                          InitialInterval = TimeSpan.FromMinutes(10),
                          BackoffCoefficient = 1f,
                      },
                  });

                // Transfeable asset ensure balance
                await ExecuteActivityAsync(
                 (BlockchainActivities x) => x.EnsureSufficientBalanceAsync(
                     context.NetworkName,
                     preparedTransaction.CallDataAsset,
                     context.FromAddress!,
                     preparedTransaction.CallDataAmount),
                  new()
                  {
                      ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                      StartToCloseTimeout = TimeSpan.FromHours(1),
                      RetryPolicy = new()
                      {
                          InitialInterval = TimeSpan.FromMinutes(10),
                          BackoffCoefficient = 1f,
                      },
                  });
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
                    await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((x) => x.RunAsync(new()
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
            //If already redeemed
            else if (ex.InnerException is ApplicationFailureException appFailException && appFailException.HasError<HashlockAlreadySetException>())
            {
                if (context.Fee == null)
                {
                    Log.Error("Hashlock already set, first attempt");
                    throw;
                }

                return context.Fee;
            }
            // if lock already exists
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<HTLCAlreadyExistsException>())
            {
                var confirmedTransaction = await GetTransactionReceiptAsync(context);
                if (confirmedTransaction != null)
                {
                    return context.Fee!;
                }
            }

            throw;
        }
    }

    private async Task CheckAllowanceAsync(
        TransactionContext context)
    {
        var lockRequest = context.PrepareArgs.FromArgs<HTLCLockTransactionPrepareRequest>();

        // Get spender address
        var spenderAddress = await ExecuteActivityAsync(
            (EVMActivities x) => x.GetSpenderAddressAsync(lockRequest.SourceNetwork, lockRequest.SourceAsset),
                Constants.DefaultActivityOptions);

        // Check allowance
        var allowance = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.GetAllowanceAsync(
                lockRequest.SourceNetwork,
                lockRequest.SourceAsset,
                context.FromAddress,
                spenderAddress),
                Constants.DefaultActivityOptions);

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction
            await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((x) => x.RunAsync(new()
            {
                PrepareArgs = new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }.ToArgs(),
                Type = TransactionType.Approve,
                UniquenessToken = Guid.NewGuid().ToString(),
                CorrelationId = context.CorrelationId ?? Guid.NewGuid().ToString(),
                FromAddress = context.FromAddress,
                NetworkName = lockRequest.SourceNetwork,
            }));
        }
    }

    private async Task<TransactionModel> GetTransactionReceiptAsync(TransactionContext context)
    {
        try
        {
            return await ExecuteActivityAsync(
                (EVMActivities x) => x.GetTransactionReceiptAsync(
                context.NetworkName,
                    context.PublishedTransactionIds),
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
                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.RunAsync(context));
            }
            else if (ex.InnerException is ApplicationFailureException appEx && appEx.HasError<TransactionFailedException>())
            {
                throw new ApplicationFailureException("Transaction failed");
            }

            throw;
        }
    }
}
