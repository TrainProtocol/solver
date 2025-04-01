using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Exceptions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows.Worklows;

[Workflow]
public class SwapWorkflow
{
    private static readonly TimeSpan _maxAcceptableCommitTimelockPeriod = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan _minAcceptableTimelockPeriod = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _defaultLPTimelockPeriod = _maxAcceptableCommitTimelockPeriod * 2;
    private static readonly TimeSpan _deafultRewardPeriod = TimeSpan.FromMinutes(30);

    private HTLCLockEventMessage? _htlcLockMessage;
    private HTLCCommitEventMessage? _htlcCommitMessage;
    private AddLockSignatureRequest? _htlcAddLockSigMessage;
    private string? _solverManagedAccountInDestination;
    private string? _solverManagedAccountInSource;
    private string? _swapId;
    private DateTimeOffset _lpTimeLock;

    [WorkflowRun]
    public async Task RunAsync(HTLCCommitEventMessage message)
    {
        _htlcCommitMessage = message;

        // Validate timelock
        var workflowStartNow = new DateTimeOffset(UtcNow);

        if (_htlcCommitMessage.TimeLock > workflowStartNow.Add(_maxAcceptableCommitTimelockPeriod).ToUnixTimeSeconds())
        {
            throw new ApplicationFailureException("Timelock is longer than max acceptable value");
        }

        var remainingTimeLockPeriodInSeconds = _htlcCommitMessage.TimeLock - workflowStartNow.ToUnixTimeSeconds();

        if (remainingTimeLockPeriodInSeconds < _minAcceptableTimelockPeriod.TotalSeconds)
        {
            throw new ApplicationFailureException("Timelock remaining time is less than min acceptable value");
        }

        var solverAddresses = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetSolverAddressesAsync(
                _htlcCommitMessage.SourceNetwork, _htlcCommitMessage.DestinationNetwork),
                       TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        _solverManagedAccountInDestination = solverAddresses[_htlcCommitMessage.DestinationNetwork];
        _solverManagedAccountInSource = solverAddresses[_htlcCommitMessage.SourceNetwork];

        // Validate limit
        var limit = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetLimitAsync(new()
            {
                SourceToken = _htlcCommitMessage.SourceAsset,
                SourceNetwork = _htlcCommitMessage.SourceNetwork,
                DestinationToken = _htlcCommitMessage.DestinationAsset,
                DestinationNetwork = _htlcCommitMessage.DestinationNetwork,
            }),
            new()
            {
                ScheduleToCloseTimeout = TimeSpan.FromDays(2),
                StartToCloseTimeout = TimeSpan.FromHours(1),
                RetryPolicy = new()
                {
                    NonRetryableErrorTypes =
                    [
                        typeof(RouteNotFoundException).Name
                    ]
                },
            });

        if (_htlcCommitMessage.Amount > limit.MaxAmount)
        {
            throw new ApplicationFailureException($"Amount is greater than max amount");
        }

        // Get quote
        var quote = await ExecuteActivityAsync(
           (SwapActivities x) => x.GetQuoteAsync(new()
           {
               SourceToken = _htlcCommitMessage.SourceAsset,
               SourceNetwork = _htlcCommitMessage.SourceNetwork,
               DestinationToken = _htlcCommitMessage.DestinationAsset,
               DestinationNetwork = _htlcCommitMessage.DestinationNetwork,
               Amount = _htlcCommitMessage.Amount
           }), TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        if (quote.ReceiveAmount <= 0)
        {
            throw new ApplicationFailureException("Output amount is less than the fee");
        }

        // Generate hashlock       
        var hashlock = await ExecuteLocalActivityAsync(
            (SwapActivities x) =>
                x.GenerateHashlockAsync(),
                new()
                {
                    StartToCloseTimeout = TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue).StartToCloseTimeout,
                    ScheduleToCloseTimeout = TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue).ScheduleToCloseTimeout,
                });

        // Create swap 
        _swapId = await ExecuteActivityAsync(
            (SwapActivities x) => x.CreateSwapAsync(_htlcCommitMessage, quote.ReceiveAmount, quote.TotalFee, hashlock.Hash),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        _lpTimeLock = new DateTimeOffset(UtcNow.Add(_defaultLPTimelockPeriod));
        var rewardTimelock = new DateTimeOffset(UtcNow.Add(_deafultRewardPeriod));

        // Lock in destination network
        await ExecuteTransactionAsync(new TransactionRequest()
        {
            PrepareArgs = JsonSerializer.Serialize(new HTLCLockTransactionPrepareRequest
            {
                SourceAsset = _htlcCommitMessage.DestinationAsset,
                DestinationAsset = _htlcCommitMessage.SourceAsset,
                DestinationNetwork = _htlcCommitMessage.SourceNetwork,
                DestinationAddress = _solverManagedAccountInSource,
                SourceNetwork = _htlcCommitMessage.DestinationNetwork,
                Amount = quote.ReceiveAmount,
                Id = _htlcCommitMessage.Id,
                Timelock = _lpTimeLock.ToUnixTimeSeconds(),
                RewardTimelock = rewardTimelock.ToUnixTimeSeconds(),
                Reward = 0,
                Receiver = _htlcCommitMessage.DestinationAddress,
                Hashlock = hashlock.Hash,
            }),
            Type = TransactionType.HTLCLock,
            NetworkName = _htlcCommitMessage.DestinationNetwork,
            NetworkType = _htlcCommitMessage.DestinationNetworkType,
            FromAddress = _solverManagedAccountInDestination!,
            SwapId = _swapId,
        });

        //_isLpLocked = true;

        try
        {
            // Wait for unlock signal or expiry
            var userLocked = await WaitConditionAsync(
                    () => _htlcLockMessage != null || _htlcAddLockSigMessage != null,
                    timeout: _lpTimeLock - UtcNow);

            if (_htlcAddLockSigMessage != null)
            {
                // Lock in source on behalf of user using signature

                var remainingAddLockTimeLockPeriodInSeconds = _htlcAddLockSigMessage.Timelock - new DateTimeOffset(UtcNow).ToUnixTimeSeconds();

                if (remainingAddLockTimeLockPeriodInSeconds < _minAcceptableTimelockPeriod.TotalSeconds)
                {
                    throw new ApplicationFailureException("Timelock remaining time is less than min acceptable value");
                }

                var childWorkflowTask = ExecuteTransactionAsync(new TransactionRequest()
                {
                    PrepareArgs = JsonSerializer.Serialize(new AddLockSigTransactionPrepareRequest
                    {
                        Id = _htlcCommitMessage.Id,
                        Hashlock = hashlock.Hash,
                        Signature = _htlcAddLockSigMessage.Signature,
                        SignatureArray = _htlcAddLockSigMessage.SignatureArray,
                        Timelock = _htlcAddLockSigMessage.Timelock,
                        R = _htlcAddLockSigMessage.R,
                        S = _htlcAddLockSigMessage.S,
                        V = _htlcAddLockSigMessage.V,
                        Asset = _htlcCommitMessage.SourceAsset
                    }),
                    Type = TransactionType.HTLCAddLockSig,
                    NetworkName = _htlcCommitMessage.SourceNetwork,
                    NetworkType = _htlcCommitMessage.SourceNetworkType,
                    FromAddress = _solverManagedAccountInSource!,
                    SwapId = _swapId
                });

                var conditionTask = WaitConditionAsync(
                    () => _htlcLockMessage != null,
                    timeout: _lpTimeLock - UtcNow);

                await Task.WhenAll(childWorkflowTask, conditionTask);

                userLocked = conditionTask.Result;
            }

            if (!userLocked
                || _htlcLockMessage!.HashLock != hashlock.Hash
                || _htlcLockMessage.TimeLock - new DateTimeOffset(UtcNow).ToUnixTimeSeconds()
                    < _minAcceptableTimelockPeriod.TotalSeconds)
            {
                // Refund LP funds
                await RefundSolverLockedFundsAsync();
                return;
            }

            // Redeem user funds
            var redeemInDestinationTask = ExecuteTransactionAsync(new TransactionRequest()
            {
                PrepareArgs = JsonSerializer.Serialize(new HTLCRedeemTransactionPrepareRequest
                {
                    Id = _htlcCommitMessage!.Id,
                    Asset = _htlcCommitMessage.DestinationAsset,
                    Secret = hashlock.Secret,
                    DestinationAddress = _htlcCommitMessage.DestinationAddress,
                    SenderAddress = _solverManagedAccountInDestination
                }),
                Type = TransactionType.HTLCRedeem,
                NetworkName = _htlcCommitMessage.DestinationNetwork,
                NetworkType = _htlcCommitMessage.DestinationNetworkType,
                FromAddress = _solverManagedAccountInDestination!,
                SwapId = _swapId
            });

            // Redeem LP funds
            var redeemInSourceTask = ExecuteTransactionAsync(new TransactionRequest()
            {
                PrepareArgs = JsonSerializer.Serialize(new HTLCRedeemTransactionPrepareRequest
                {
                    Id = _htlcCommitMessage!.Id,
                    Asset = _htlcCommitMessage.SourceAsset,
                    Secret = hashlock.Secret,
                    DestinationAddress = _solverManagedAccountInSource,
                    SenderAddress = _htlcCommitMessage.SenderAddress
                }),
                Type = TransactionType.HTLCRedeem,
                NetworkName = _htlcCommitMessage.SourceNetwork,
                NetworkType = _htlcCommitMessage.SourceNetworkType,
                FromAddress = _solverManagedAccountInSource!,
                SwapId = _swapId
            });

            await Task.WhenAll(
                redeemInDestinationTask,
                redeemInSourceTask);

        }
        catch (Exception e) when (TemporalException.IsCanceledException(e))
        {
            await RefundSolverLockedFundsAsync();
            throw;
        }
    }

    private async Task RefundSolverLockedFundsAsync()
    {
        var diff = _lpTimeLock - new DateTimeOffset(UtcNow);

        if (diff.TotalSeconds >= 0)
        {
            await DelayAsync(diff);
        }

        await ExecuteTransactionAsync(new TransactionRequest()
        {
            PrepareArgs = JsonSerializer.Serialize(new HTLCRefundTransactionPrepareRequest
            {
                Id = _htlcCommitMessage!.Id,
                Asset = _htlcCommitMessage.DestinationAsset,
            }),
            Type = TransactionType.HTLCRefund,
            NetworkName = _htlcCommitMessage.DestinationNetwork,
            NetworkType = _htlcCommitMessage.DestinationNetworkType,
            FromAddress = _solverManagedAccountInDestination!,
            SwapId = _swapId!
        });
    }

    [WorkflowUpdate]
    public async Task<bool> SetAddLockSigAsync(AddLockSignatureRequest addLockSig)
    {
        try
        {
            var isValid = await ExecuteActivityAsync<bool>(
                "ValidateAddLockSignature",
                [
                    addLockSig
                ],
                TemporalHelper.DefaultActivityOptions(
                    TemporalHelper.ResolveBlockchainActivityTaskQueue(
                        _htlcCommitMessage!.SourceNetworkType))); // Todo: temp workaround

            if (isValid)
            {
                _htlcAddLockSigMessage = addLockSig;
            }

            return isValid;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [WorkflowSignal]
    public Task LockCommitedAsync(HTLCLockEventMessage message)
    {
        _htlcLockMessage = message;
        return Task.CompletedTask;
    }

    private async Task<TransactionResponse> ExecuteTransactionAsync(TransactionRequest transactionRequest)
    {
        var confirmedTransaction = await ExecuteChildWorkflowAsync<TransactionResponse>(
            TemporalHelper.ResolveProcessor(transactionRequest.NetworkType),
            [transactionRequest, new TransactionExecutionContext()],
            new ChildWorkflowOptions
            {
                Id = TemporalHelper.BuildProcessorId(
                    transactionRequest.NetworkName,
                    transactionRequest.Type,
                    NewGuid()),
                TaskQueue = transactionRequest.NetworkType.ToString(),
            });

        await ExecuteActivityAsync(
            (SwapActivities x) =>
                x.CreateSwapTransactionAsync(transactionRequest.SwapId, transactionRequest.Type, confirmedTransaction),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        await ExecuteActivityAsync(
            (SwapActivities x) => x.UpdateExpensesAsync(
                confirmedTransaction.NetworkName,
                confirmedTransaction.FeeAsset,
                confirmedTransaction.FeeAmount,
                confirmedTransaction.Asset,
                transactionRequest.Type),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        return confirmedTransaction;
    }
}