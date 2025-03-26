using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Blockchains.EVM.Activities;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Models.HTLCModels;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Extensions;
using Train.Solver.Core.Workflows.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchains.EVM.Workflows;

[Workflow]
public class EVMTransactionProcessor
{
    const int MaxRetryCount = 5;

    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionContext context)
    {
        // Check allowance
        if (context.Type == TransactionType.HTLCLock)
        {
            await CheckAllowanceAsync(context);
        }

        // Prepare transaction
        var preparedTransaction = await ExecuteActivityAsync<PrepareTransactionResponse>(
            $"{context.NetworkType}{nameof(IBlockchainActivities.BuildTransactionAsync)}",
            [
                new TransactionBuilderRequest()
                {
                    NetworkName = context.NetworkName,
                    Args = context.PrepareArgs,
                    Type = context.Type
                }
            ],
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        // Estimate fee
        if (context.Fee == null)
        {
            context.Fee = await GetFeeAsync(context, preparedTransaction);
        }

        // Get nonce
        if (string.IsNullOrEmpty(context.Nonce))
        {
            context.Nonce = await ExecuteActivityAsync<string>(
                $"{context.NetworkType}{nameof(IBlockchainActivities.GetReservedNonceAsync)}",
                [
                    new ReservedNonceRequest()
                    {
                        NetworkName = context.NetworkName,
                        Address = context.FromAddress!,
                        ReferenceId = context.UniquenessToken
                    }
                ],
                TemporalHelper.DefaultActivityOptions(context.NetworkType));
        }

        var rawTransaction = await ExecuteActivityAsync<SignedTransaction>(
            $"{context.NetworkType}{nameof(IEVMBlockchainActivities.ComposeSignedRawTransactionAsync)}",
            [
                new EVMComposeTransactionRequest()
                {
                    NetworkName = context.NetworkName,
                    FromAddress = context.FromAddress,
                    ToAddress = preparedTransaction.ToAddress,
                    Nonce = context.Nonce,
                    AmountInWei = preparedTransaction.AmountInWei,
                    CallData = preparedTransaction.Data,
                    Fee = context.Fee
                }
            ],
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        // Initiate blockchain transfer
        try
        {
            var txId = await ExecuteActivityAsync<string>(
                $"{context.NetworkType}{nameof(IEVMBlockchainActivities.PublishRawTransactionAsync)}",
                [
                    new EVMPublishTransactionRequest()
                    {
                        NetworkName = context.NetworkName,
                        FromAddress = context.FromAddress,
                        SignedTransaction = rawTransaction
                    }
                ],
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
                    $"{context.NetworkType}{nameof(IEVMBlockchainActivities.IncreaseFeeAsync)}",
                    [
                        new EVMFeeIncreaseRequest()
                        {
                            Fee = newFee,
                            NetworkName = context.NetworkName,
                        }
                    ],
                    TemporalHelper.DefaultActivityOptions(context.NetworkType));

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
            var fee = await ExecuteActivityAsync<Fee>(
                $"{context.NetworkType}{nameof(IEVMBlockchainActivities.EstimateFeeAsync)}",
                [
                    new EstimateFeeRequest
                    {
                        NetworkName = context.NetworkName,
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
                    $"{context.NetworkType}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",
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
                    $"{context.NetworkType}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",
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
                        RetryPolicy = new()
                        {
                            InitialInterval = TimeSpan.FromMinutes(10),
                            BackoffCoefficient = 1f,
                        },
                    });

                // Transfeable asset ensure balance
                await ExecuteActivityAsync(
                    $"{context.NetworkType}{nameof(IBlockchainActivities.EnsureSufficientBalanceAsync)}",
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
                    await ExecuteChildWorkflowAsync(nameof(EVMTransactionProcessor), [new TransactionContext()
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
                        }),
                        Type = TransactionType.Transfer,
                        SwapId = context.SwapId,
                    }], new() { Id = TemporalHelper.BuildId(context.NetworkName, TransactionType.Transfer, NewGuid()) });
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
        var lockRequest = JsonSerializer.Deserialize<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

        if (lockRequest is null)
        {
            throw new Exception($"Occured exception during deserializing {context.PrepareArgs}");
        }

        // Get spender address
        var spenderAddress = await ExecuteActivityAsync<string>(
            $"{context.NetworkType}{nameof(IEVMBlockchainActivities.GetSpenderAddressAsync)}",
            [
                new SpenderAddressRequest()
                {
                    Asset = lockRequest.SourceAsset,
                    NetworkName = lockRequest.SourceNetwork,
                }
            ],
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        // Check allowance
        var allowance = await ExecuteActivityAsync<decimal>(
            $"{context.NetworkType}{nameof(IEVMBlockchainActivities.GetSpenderAllowanceAsync)}",
            [
               new AllowanceRequest()
               {
                   NetworkName = lockRequest.SourceNetwork,
                   OwnerAddress = context.FromAddress,
                   SpenderAddress = spenderAddress,
                   Asset = lockRequest.SourceAsset

               }
            ],
            TemporalHelper.DefaultActivityOptions(context.NetworkType));

        if (lockRequest.Amount > allowance)
        {
            // Initiate approval transaction

            await ExecuteChildWorkflowAsync(nameof(EVMTransactionProcessor), [new TransactionContext()
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
                NetworkType = context.NetworkType,
                SwapId = context.SwapId,
            }], new() { Id = TemporalHelper.BuildId(context.NetworkName, TransactionType.Approve, NewGuid()) });

        }
    }

    private async Task<TransactionResponse> GetTransactionReceiptAsync(TransactionContext context)
    {
        try
        {
            return await ExecuteActivityAsync<TransactionResponse>(
               $"{context.NetworkType}{nameof(IEVMBlockchainActivities.GetBatchTransactionAsync)}",
                [
                    new GetBatchTransactionRequest()
                    {
                        NetworkName = context.NetworkName,
                        TransactionIds = context.PublishedTransactionIds.ToArray()
                    }
                ],
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
