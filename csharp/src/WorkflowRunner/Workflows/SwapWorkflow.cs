using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Temporal.Abstractions;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Activities;
using Train.Solver.WorkflowRunner.Exceptions;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class SwapWorkflow : ISwapWorkflow
{
    private static readonly TimeSpan _maxAcceptableCommitTimelockPeriod = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan _minAcceptableTimelockPeriod = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _defaultLPTimelockPeriod = _maxAcceptableCommitTimelockPeriod * 2;
    private static readonly TimeSpan _deafultRewardPeriod = TimeSpan.FromMinutes(30);

    private HTLCLockEventMessage? _htlcLockMessage;
    private HTLCCommitEventMessage? _htlcCommitMessage;
    private AddLockSigRequest? _htlcAddLockSigMessage;
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
                       Constants.DefaultActivityOptions);

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
           }), Constants.DefaultActivityOptions);

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
                    StartToCloseTimeout = Constants.DefaultActivityOptions.StartToCloseTimeout,
                    ScheduleToCloseTimeout = Constants.DefaultActivityOptions.ScheduleToCloseTimeout,
                });

        // Create swap 
        _swapId = await ExecuteActivityAsync(
            (SwapActivities x) => x.CreateSwapAsync(_htlcCommitMessage, quote.ReceiveAmount, quote.TotalFee, hashlock.Hash),
                Constants.DefaultActivityOptions);

        _lpTimeLock = new DateTimeOffset(UtcNow.Add(_defaultLPTimelockPeriod));
        var rewardTimelock = new DateTimeOffset(UtcNow.Add(_deafultRewardPeriod));

        //_ = Task.Run(async () =>
        //{
        //    await DelayAsync(_lpTimeLock - new DateTimeOffset(UtcNow));

        //    if (_isLpLocked)
        //    {
        //        await RefundSolverLockedFundsAsync();
        //    }

        //    throw new ApplicationFailureException("Timelock expired. Workflow terminated in background.");
        //});

        // Lock in destination network
        await ExecuteChildWorkflowAsync<TransactionWorkflow>((x) => x.ExecuteTransactionAsync(new()
        {
            PrepareArgs = new HTLCLockTransactionPrepareRequest
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
            }.ToArgs(),
            Type = TransactionType.HTLCLock,
            CorrelationId = _htlcCommitMessage.Id,
            NetworkName = _htlcCommitMessage.DestinationNetwork,
            FromAddress = _solverManagedAccountInDestination!,
        }, _swapId), new() { Id = TransactionWorkflow.BuildId(_htlcCommitMessage.DestinationNetwork, TransactionType.HTLCLock) });

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

                var childWorkflowTask = ExecuteChildWorkflowAsync((TransactionWorkflow x) => x.ExecuteTransactionAsync(new()
                {
                    PrepareArgs = new HTLCAddLockSigTransactionPrepareRequest
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
                    }.ToArgs(),
                    Type = TransactionType.HTLCAddLockSig,
                    CorrelationId = _htlcCommitMessage.Id,
                    NetworkName = _htlcCommitMessage.SourceNetwork,
                    FromAddress = _solverManagedAccountInSource!,
                }, _swapId), new() { Id = TransactionWorkflow.BuildId(_htlcCommitMessage.SourceNetwork, TransactionType.HTLCAddLockSig) });

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
            var redeemInDestinationTask = ExecuteChildWorkflowAsync((TransactionWorkflow x) => x.ExecuteTransactionAsync(new()
            {
                PrepareArgs = new HTLCRedeemTransactionPrepareRequest
                {
                    Id = _htlcCommitMessage!.Id,
                    Asset = _htlcCommitMessage.DestinationAsset,
                    Secret = hashlock.Secret,
                    DestinationAddress = _htlcCommitMessage.DestinationAddress,
                    SenderAddress = _solverManagedAccountInDestination
                }.ToArgs(),
                Type = TransactionType.HTLCRedeem,
                CorrelationId = _htlcCommitMessage.Id,
                NetworkName = _htlcCommitMessage.DestinationNetwork,
                FromAddress = _solverManagedAccountInDestination!,
            }, _swapId), new() { Id = TransactionWorkflow.BuildId(_htlcCommitMessage.DestinationNetwork, TransactionType.HTLCRedeem) });

            // Redeem LP funds
            var redeemInSourceTask = ExecuteChildWorkflowAsync((TransactionWorkflow x) => x.ExecuteTransactionAsync(new()
            {
                PrepareArgs = new HTLCRedeemTransactionPrepareRequest
                {
                    Id = _htlcCommitMessage!.Id,
                    Asset = _htlcCommitMessage.SourceAsset,
                    Secret = hashlock.Secret,
                    DestinationAddress = _solverManagedAccountInSource,
                    SenderAddress = _htlcCommitMessage.SenderAddress
                }.ToArgs(),
                Type = TransactionType.HTLCRedeem,
                CorrelationId = _htlcCommitMessage.Id,
                NetworkName = _htlcCommitMessage.SourceNetwork,
                FromAddress = _solverManagedAccountInSource!,
            }, _swapId), new() { Id = TransactionWorkflow.BuildId(_htlcCommitMessage.SourceNetwork, TransactionType.HTLCRedeem) });

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

        await ExecuteChildWorkflowAsync((TransactionWorkflow x) => x.ExecuteTransactionAsync(
            new()
            {
                PrepareArgs = new HTLCRefundTransactionPrepareRequest
                {
                    Id = _htlcCommitMessage!.Id,
                    Asset = _htlcCommitMessage.DestinationAsset,
                }.ToArgs(),
                Type = TransactionType.HTLCRefund,
                CorrelationId = _htlcCommitMessage.Id,
                NetworkName = _htlcCommitMessage.DestinationNetwork,
                FromAddress = _solverManagedAccountInDestination!,
            }, _swapId), new() { Id = TransactionWorkflow.BuildId(_htlcCommitMessage.DestinationNetwork, TransactionType.HTLCRefund) });
    }

    [WorkflowSignal]
    public Task LockCommitedAsync(HTLCLockEventMessage message)
    {
        _htlcLockMessage = message;
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task AddLockSignatureAsync(AddLockSigRequest addlockSigMessage)
    {
        _htlcAddLockSigMessage = addlockSigMessage;
        return Task.CompletedTask;
    }
}
