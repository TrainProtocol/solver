using System.Numerics;
using System.Text.Json;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Swap.Activities;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Enums;
using static Temporalio.Workflows.Workflow;
using static Train.Solver.Blockchain.Common.Helpers.TemporalHelper;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class SwapWorkflow : ISwapWorkflow
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
    private DetailedNetworkDto? _sourceNetwork;
    private DetailedNetworkDto? _destinationNetwork;
    private int? _swapId;
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

        _sourceNetwork = await ExecuteActivityAsync(
           (INetworkActivities x) => x.GetNetworkAsync(_htlcCommitMessage.SourceNetwork),
           DefaultActivityOptions(Constants.CoreTaskQueue));

        _destinationNetwork = await ExecuteActivityAsync(
            (INetworkActivities x) => x.GetNetworkAsync(_htlcCommitMessage.DestinationNetwork),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        if (!_sourceNetwork.Tokens.Any(x => x.Symbol == message.SourceAsset) || !_destinationNetwork.Tokens.Any(x => x.Symbol == message.DestinationAsset))
        {
            throw new ApplicationFailureException("Source or destination asset is not supported in the network");
        }

        _solverManagedAccountInDestination = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetSolverAddressAsync(
                _destinationNetwork.Type),
                       DefaultActivityOptions(Constants.CoreTaskQueue));

        _solverManagedAccountInSource = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetSolverAddressAsync(
                _sourceNetwork.Type),
                       DefaultActivityOptions(Constants.CoreTaskQueue));

        // Validate limit
        var limit = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetLimitAsync(new()
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

        if (BigInteger.Parse(_htlcCommitMessage.AmountInWei) > limit.MaxAmount)
        {
            throw new ApplicationFailureException($"Amount is greater than max amount");
        }

        // Get quote
        var quote = await ExecuteActivityAsync(
           (ISwapActivities x) => x.GetQuoteAsync(new()
           {
               SourceToken = _htlcCommitMessage.SourceAsset,
               SourceNetwork = _htlcCommitMessage.SourceNetwork,
               DestinationToken = _htlcCommitMessage.DestinationAsset,
               DestinationNetwork = _htlcCommitMessage.DestinationNetwork,
               Amount = BigInteger.Parse(_htlcCommitMessage.AmountInWei)
           }), DefaultActivityOptions(Constants.CoreTaskQueue));

        if (quote.ReceiveAmount <= 0)
        {
            throw new ApplicationFailureException("Output amount is less than the fee");
        }

        // Generate hashlock       
        var hashlock = await ExecuteLocalActivityAsync(
            (ISwapActivities x) =>
                x.GenerateHashlockAsync(),
                new()
                {
                    StartToCloseTimeout = DefaultActivityOptions(Constants.CoreTaskQueue).StartToCloseTimeout,
                    ScheduleToCloseTimeout = DefaultActivityOptions(Constants.CoreTaskQueue).ScheduleToCloseTimeout,
                });

        // Create swap 
        _swapId = await ExecuteActivityAsync(
            (ISwapActivities x) => x.CreateSwapAsync(_htlcCommitMessage, quote.ReceiveAmount.ToString(), quote.TotalFee.ToString(), hashlock.Hash),
                DefaultActivityOptions(Constants.CoreTaskQueue));

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
                Amount = quote.ReceiveAmount.ToString(),
                Id = _htlcCommitMessage.Id,
                Timelock = _lpTimeLock.ToUnixTimeSeconds(),
                RewardTimelock = rewardTimelock.ToUnixTimeSeconds(),
                Reward = BigInteger.Zero.ToString(),
                Receiver = _htlcCommitMessage.DestinationAddress,
                Hashlock = hashlock.Hash,
            }),
            Type = TransactionType.HTLCLock,
            Network = _destinationNetwork,
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
                        Asset = _htlcCommitMessage.SourceAsset,
                        SignerAddress = _htlcAddLockSigMessage.SignerAddress,
                    }),
                    Type = TransactionType.HTLCAddLockSig,
                    Network = _sourceNetwork,
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
                await RefundSolverLockedFundsAsync(_destinationNetwork);
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
                Network = _destinationNetwork,
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
                Network = _sourceNetwork,
                FromAddress = _solverManagedAccountInSource!,
                SwapId = _swapId
            });

            await Task.WhenAll(
                redeemInDestinationTask,
                redeemInSourceTask);

        }
        catch (Exception e) when (TemporalException.IsCanceledException(e))
        {
            await RefundSolverLockedFundsAsync(_destinationNetwork);
            throw;
        }
    }

    private async Task RefundSolverLockedFundsAsync(DetailedNetworkDto network)
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
            Network = network,
            FromAddress = _solverManagedAccountInDestination!,
            SwapId = _swapId!
        });
    }

    [WorkflowUpdate]
    public async Task<bool> SetAddLockSigAsync(AddLockSignatureRequest addLockSig)
    {
        if (_htlcAddLockSigMessage != null)
        {
            return true;
        }

        try
        {
            var isValid = await ExecuteActivityAsync(
                (IBlockchainActivities x) => x.ValidateAddLockSignatureAsync(addLockSig),
                DefaultActivityOptions(_sourceNetwork.Type));

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
        if (_htlcLockMessage == null)
        {
            _htlcLockMessage = message;
        }

        return Task.CompletedTask;
    }

    private async Task<TransactionResponse> ExecuteTransactionAsync(TransactionRequest transactionRequest)
    {
        var confirmedTransaction = await ExecuteChildTransactionProcessorWorkflowAsync(
            transactionRequest.Network.Type,
            x => x.RunAsync(transactionRequest, new TransactionExecutionContext()),
            new ChildWorkflowOptions
            {
                Id = BuildProcessorId(
                    transactionRequest.Network.Name,
                    transactionRequest.Type,
                    NewGuid()),
                TaskQueue = transactionRequest.Network.Type.ToString(),
            });

        await ExecuteActivityAsync(
            (ISwapActivities x) =>
                x.CreateSwapTransactionAsync(transactionRequest.SwapId, transactionRequest.Type, confirmedTransaction),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        await ExecuteActivityAsync(
            (ISwapActivities x) => x.UpdateExpensesAsync(
                confirmedTransaction.NetworkName,
                confirmedTransaction.FeeAsset,
                confirmedTransaction.FeeAmount,
                confirmedTransaction.Asset,
                transactionRequest.Type),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        return confirmedTransaction;
    }
}
