using System.Numerics;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Common.Enums;
using static Temporalio.Workflows.Workflow;
using static Train.Solver.Workflow.Common.Helpers.TemporalHelper;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Common.Extensions;

namespace Train.Solver.Workflow.Swap.Workflows;

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
    private string? _destinationWalletAddress;
    private string? _destinationWalletAgentUrl;
    private string? _sourceWalletAddress;
    private string? _sourceWalletAgentUrl;
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
           DefaultActivityOptions(Constants.CoreTaskQueue, $"Getting source network {_htlcCommitMessage.SourceNetwork}"));

        _destinationNetwork = await ExecuteActivityAsync(
            (INetworkActivities x) => x.GetNetworkAsync(_htlcCommitMessage.DestinationNetwork),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        if (!_sourceNetwork.Tokens.Any(x => x.Symbol == message.SourceAsset) || !_destinationNetwork.Tokens.Any(x => x.Symbol == message.DestinationAsset))
        {
            throw new ApplicationFailureException("Source or destination asset is not supported in the network");
        }

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

        if (_htlcCommitMessage.Amount > limit.MaxAmount)
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
               Amount = _htlcCommitMessage.Amount
           }), DefaultActivityOptions(Constants.CoreTaskQueue));

        if (quote.ReceiveAmount <= 0)
        {
            throw new ApplicationFailureException("Output amount is less than the fee");
        }

        _destinationWalletAddress = quote.DestinationSolverAddress;
        _sourceWalletAddress = quote.SourceSolverAddress;

        var sourceWalletAgent = await ExecuteActivityAsync(
            (IWalletActivities x) => x.GetSignerAgentAsync(quote.SourceSignerAgent),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        _sourceWalletAgentUrl = sourceWalletAgent.Url;

        var destinationWalletAgent = await ExecuteActivityAsync(
            (IWalletActivities x) => x.GetSignerAgentAsync(quote.DestinationSignerAgent),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        _destinationWalletAgentUrl = destinationWalletAgent.Url;

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
            PrepareArgs = new HTLCLockTransactionPrepareRequest
            {
                SourceAsset = _htlcCommitMessage.DestinationAsset,
                DestinationAsset = _htlcCommitMessage.SourceAsset,
                DestinationNetwork = _htlcCommitMessage.SourceNetwork,
                DestinationAddress = _sourceWalletAddress,
                SourceNetwork = _htlcCommitMessage.DestinationNetwork,
                Amount = quote.ReceiveAmount,
                CommitId = _htlcCommitMessage.CommitId,
                Timelock = _lpTimeLock.ToUnixTimeSeconds(),
                RewardTimelock = rewardTimelock.ToUnixTimeSeconds(),
                Reward = BigInteger.Zero,
                Receiver = _htlcCommitMessage.DestinationAddress,
                Hashlock = hashlock.Hash,
            }.ToJson(),
            Type = TransactionType.HTLCLock,
            Network = _destinationNetwork,
            FromAddress = _destinationWalletAddress!,
            SignerAgentUrl = _destinationWalletAgentUrl!,
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

                var remainingTimePeriodBetweenAddLockSigAndSolverLockInSeconds = _lpTimeLock.ToUnixTimeSeconds() - _htlcAddLockSigMessage.Timelock;

                if (remainingTimePeriodBetweenAddLockSigAndSolverLockInSeconds < _minAcceptableTimelockPeriod.TotalSeconds)
                {
                    throw new ApplicationFailureException("Timelock remaining time is less than solver lock");
                }

                await ExecuteTransactionAsync(new TransactionRequest()
                {
                    PrepareArgs = new AddLockSigTransactionPrepareRequest
                    {
                        CommitId = _htlcCommitMessage.CommitId,
                        Hashlock = hashlock.Hash,
                        Signature = _htlcAddLockSigMessage.Signature,
                        SignatureArray = _htlcAddLockSigMessage.SignatureArray,
                        Timelock = _htlcAddLockSigMessage.Timelock,
                        R = _htlcAddLockSigMessage.R,
                        S = _htlcAddLockSigMessage.S,
                        V = _htlcAddLockSigMessage.V,
                        Asset = _htlcCommitMessage.SourceAsset,
                        SignerAddress = _htlcAddLockSigMessage.SignerAddress,
                    }.ToJson(),
                    Type = TransactionType.HTLCAddLockSig,
                    Network = _sourceNetwork,
                    FromAddress = _sourceWalletAddress!,
                    SignerAgentUrl = _sourceWalletAgentUrl!,
                    SwapId = _swapId
                });

                userLocked = await WaitConditionAsync(
                    () => _htlcLockMessage != null,
                    timeout: _lpTimeLock - UtcNow);
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

            var tasks = new List<Task>();

            if (_destinationNetwork.Type != NetworkType.Aztec)
            {
                // Redeem user funds
                var redeemInDestinationTask = ExecuteTransactionAsync(new TransactionRequest()
                {
                    PrepareArgs = new HTLCRedeemTransactionPrepareRequest
                    {
                        CommitId = _htlcCommitMessage!.CommitId,
                        Asset = _htlcCommitMessage.DestinationAsset,
                        Secret = hashlock.Secret,
                        DestinationAddress = _htlcCommitMessage.DestinationAddress,
                        SenderAddress = _destinationWalletAddress
                    }.ToJson(),
                    Type = TransactionType.HTLCRedeem,
                    Network = _destinationNetwork,
                    FromAddress = _destinationWalletAddress!,
                    SignerAgentUrl = _sourceWalletAgentUrl!,
                    SwapId = _swapId
                });

                tasks.Add(redeemInDestinationTask);
            }

            // Redeem LP funds
            var redeemInSourceTask = ExecuteTransactionAsync(new TransactionRequest()
            {
                PrepareArgs = new HTLCRedeemTransactionPrepareRequest
                {
                    CommitId = _htlcCommitMessage!.CommitId,
                    Asset = _htlcCommitMessage.SourceAsset,
                    Secret = hashlock.Secret,
                    DestinationAddress = _sourceWalletAddress,
                    SenderAddress = _htlcCommitMessage.SenderAddress
                }.ToJson(),
                Type = TransactionType.HTLCRedeem,
                Network = _sourceNetwork,
                FromAddress = _sourceWalletAddress!,
                SignerAgentUrl = _sourceWalletAgentUrl!,
                SwapId = _swapId
            });

            tasks.Add(redeemInSourceTask);

            await Task.WhenAll(tasks);

        }
        catch (Exception e) when (TemporalException.IsCanceledException(e))
        {
            await RefundSolverLockedFundsAsync(_destinationNetwork);
            throw;
        }

        await ExecuteActivityAsync(
            (ISwapActivities x) => x.CreateSwapMetricAsync(_htlcCommitMessage.CommitId, quote),
            DefaultActivityOptions(Constants.CoreTaskQueue));
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
            PrepareArgs = new HTLCRefundTransactionPrepareRequest
            {
                CommitId = _htlcCommitMessage!.CommitId,
                Asset = _htlcCommitMessage.DestinationAsset,
            }.ToJson(),
            Type = TransactionType.HTLCRefund,
            Network = network,
            FromAddress = _destinationWalletAddress!,
            SignerAgentUrl = _destinationWalletAgentUrl!,
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

    private async Task<TransactionResponse> ExecuteTransactionAsync(
        TransactionRequest transactionRequest)
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
                confirmedTransaction.FeeAmount.ToString(),
                confirmedTransaction.Asset,
                transactionRequest.Type),
            DefaultActivityOptions(Constants.CoreTaskQueue));

        return confirmedTransaction;
    }
}
