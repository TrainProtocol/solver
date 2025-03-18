using Serilog;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.EVM.Activities;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Workflows;
using Train.Solver.Data.Entities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Blockchains.EVM.Workflows;

[Workflow]
public class EVMTransactionProcessor : TransactionProcessorBase
{
    const int MaxRetryCount = 5;

    [WorkflowRun]
    public override Task<TransactionModel> RunAsync(TransactionContext transactionContext)
    {
        return base.RunAsync(transactionContext);
    }

    protected override async Task<TransactionModel> ExecuteTransactionAsync(TransactionContext context)
    {
        // Check allowance
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        // Prepare transaction
        var preparedTransaction = await ExecuteActivityAsync<PrepareTransactionResponse>(
            $"{context.NetworkGroup}{nameof(IBlockchainActivities.BuildTransactionAsync)}",
            [context.NetworkName, context.Type, context.PrepareArgs],
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        // Estimate fee
        if (context.Fee == null)
        {
            context.Fee = await GetFeeAsync(context, preparedTransaction);
        }

        // Get nonce
        if (string.IsNullOrEmpty(context.Nonce))
        {
            context.Nonce = await ExecuteActivityAsync<string>(
                $"{context.NetworkGroup}{nameof(IBlockchainActivities.GetNonceAsync)}",
                [context.NetworkName, context.FromAddress!, context.UniquenessToken],
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }

        var rawTransaction = await ExecuteActivityAsync<SignedTransaction>(
            $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.ComposeSignedRawTransactionAsync)}",
          [
              context.NetworkName,
              context.FromAddress,
              preparedTransaction.ToAddress,
              context.Nonce,
              preparedTransaction.AmountInWei,
              preparedTransaction.Data,
              context.Fee],
          TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        // Initiate blockchain transfer
        try
        {
            var txId = await ExecuteActivityAsync<string>(
                $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.PublishRawTransactionAsync)}",
                [
                    context.NetworkName,
                    context.FromAddress,
                    rawTransaction],
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

                var increasedFee = await ExecuteActivityAsync<Fee>(
                    $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.IncreaseFee)}",
                    [context.NetworkName, newFee],
                    TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

                context.Fee = increasedFee;
                context.Attempts++;

                throw CreateContinueAsNewException<EVMTransactionProcessor>((x) => x.ExecuteTransactionAsync(context));
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
            var fee = await ExecuteActivityAsync<Fee>(
                $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.EstimateFeeAsync)}",
                [context.NetworkName,
                new EstimateFeeRequest
                {
                    FromAddress = context.FromAddress!,
                    ToAddress = preparedTransaction.ToAddress!,
                    Asset = preparedTransaction.Asset!,
                    Amount = preparedTransaction.Amount,
                    CallData = preparedTransaction.Data,
                }],
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
                    $"{context.NetworkGroup}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",
                    [
                        context.NetworkName, 
                        context.FromAddress!,
                        fee.Asset!,
                        preparedTransaction.CallDataAmount + fee.Amount],
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
                    $"{context.NetworkGroup}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",
                    [
                     context.NetworkName,
                     context.FromAddress!,
                     fee.Asset,
                     fee.Amount],
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
                    $"{context.NetworkGroup}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",

                 [
                     context.NetworkName,
                     context.FromAddress!,
                     preparedTransaction.CallDataAsset,
                     preparedTransaction.CallDataAmount],
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
                    await ExecuteChildWorkflowAsync<EVMTransactionProcessor>((x) => x.RunAsync(new TransactionContext()
                    {
                        UniquenessToken = context.UniquenessToken,
                        NetworkName = context.NetworkName,
                        NetworkGroup = context.NetworkGroup,
                        Nonce = context.Nonce,
                        FromAddress = context.FromAddress,
                        PrepareArgs = new TransferPrepareRequest
                        {
                            Amount = 0,
                            Asset = context.Fee!.Asset,
                            ToAddress = context.FromAddress,
                        }.ToArgs(),
                        Type = TransactionType.Transfer,
                        SwapId = context.SwapId,
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
        var spenderAddress = await ExecuteActivityAsync<string>(
             $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.GetSpenderAddressAsync)}",
             [lockRequest.SourceNetwork, lockRequest.SourceAsset],
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        // Check allowance
        var allowance = await ExecuteActivityAsync<decimal>(
            $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.GetSpenderAllowanceAsync)}",
            [
                lockRequest.SourceNetwork,                
                context.FromAddress,
                spenderAddress,
                lockRequest.SourceAsset],
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction

            await ExecuteChildWorkflowAsync(nameof(EVMTransactionProcessor), [new TransactionContext()
            {
                PrepareArgs = new ApprovePrepareRequest
                {
                    SpenderAddress = spenderAddress,
                    Amount = 1000000000m,
                    Asset = lockRequest.SourceAsset,
                }.ToArgs(),
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
               $"{context.NetworkGroup}{nameof(IEVMBlockchainActivities.GetBatchTransactionAsync)}",
                [context.NetworkName,
                    context.PublishedTransactionIds],
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
