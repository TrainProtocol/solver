using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Common;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class EventListenerWorkflow : IEventListenerWorkflow
{
    private ulong? _lastProcessedBlockNumber;
    private const int _maxConcurrentTaskCount = 4;
    private const int _maxIterationsBeforeContinueAsNew = 200;
    private readonly HashSet<string> _processedTransacrtionHashes = [];

    [WorkflowRun]
    public async Task RunAsync(
        string networkName,
        uint blockBatchSize,
        int waitInterval,
        ulong? lastProcessedBlockNumber = null)
    {
        _lastProcessedBlockNumber = lastProcessedBlockNumber;

        var iteration = 0;

        var network = await ExecuteActivityAsync(
            (INetworkActivities x) => x.GetNetworkAsync(networkName),
            new ActivityOptions
            {
                TaskQueue = Constants.CoreTaskQueue,
                StartToCloseTimeout = TimeSpan.FromSeconds(20),
                ScheduleToCloseTimeout = TimeSpan.FromMinutes(20),
                RetryPolicy = new()
                {
                    InitialInterval = TimeSpan.FromSeconds(5),
                    BackoffCoefficient = 1f,
                }
            });

        var solverWallets = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetRouteSourceWalletsAsync(
                network.Type),
                new()
                {
                    TaskQueue = Constants.CoreTaskQueue,
                    StartToCloseTimeout = TimeSpan.FromSeconds(20),
                    ScheduleToCloseTimeout = TimeSpan.FromMinutes(20),
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(5),
                        BackoffCoefficient = 1f,
                    }
                });

        while (!Temporalio.Workflows.Workflow.CancellationToken.IsCancellationRequested)
        {
            // Reset workflow history if it has been running for too long
            if (iteration >= _maxIterationsBeforeContinueAsNew)
            {
                throw CreateContinueAsNewException<EventListenerWorkflow>((x) => x.RunAsync(
                    networkName,
                    blockBatchSize,
                    waitInterval,
                    _lastProcessedBlockNumber));
            }

            try
            {
                var blockNumberWithHash = await ExecuteActivityAsync(
                    (IBlockchainActivities x) => x.GetLastConfirmedBlockNumberAsync(
                        new BaseRequest { Network = network }),
                    new()
                    {
                        TaskQueue = network.Type.ToString(),
                        StartToCloseTimeout = TimeSpan.FromSeconds(20),
                        ScheduleToCloseTimeout = TimeSpan.FromMinutes(20),
                        RetryPolicy = new()
                        {
                            InitialInterval = TimeSpan.FromSeconds(5),
                            BackoffCoefficient = 1f,
                        }
                    });

                if (!_lastProcessedBlockNumber.HasValue)
                {
                    _lastProcessedBlockNumber = blockNumberWithHash.BlockNumber - blockBatchSize;
                }

                if (_lastProcessedBlockNumber >= blockNumberWithHash.BlockNumber)
                {
                    await DelayAsync(TimeSpan.FromSeconds(waitInterval));
                    iteration++;
                    continue;
                }

                var blockRanges = await ExecuteLocalActivityAsync(
                   (IUtilityActivities x) => x.GenerateBlockRanges(
                       _lastProcessedBlockNumber.Value - 15,
                       blockNumberWithHash.BlockNumber,
                       blockBatchSize),
                   new LocalActivityOptions
                   {
                       StartToCloseTimeout = TimeSpan.FromSeconds(5)
                   });


                if (blockRanges.Any())
                {
                    foreach (var blockChunk in blockRanges.Chunk(_maxConcurrentTaskCount))
                    {
                        await Task.WhenAll(blockChunk.Select(x => ProcessBlockRangeAsync(network, solverWallets, x)));

                        _lastProcessedBlockNumber = blockChunk.Last().To;
                    }

                }
            }
            catch (Exception)
            {
                throw CreateContinueAsNewException<EventListenerWorkflow>((x) => x.RunAsync(
                    networkName,
                    blockBatchSize,
                    waitInterval,
                    _lastProcessedBlockNumber));
            }

            iteration++;
        }
    }

    private async Task ProcessBlockRangeAsync(
        DetailedNetworkDto network,
        string[] solverWallets,
        BlockRangeModel blockRange)
    {
        var result = await ExecuteActivityAsync(
            (IBlockchainActivities x) => x.GetEventsAsync(new EventRequest()
            {
                Network = network,
                FromBlock = blockRange.From,
                ToBlock = blockRange.To,
                WalletAddresses = solverWallets,
            }),
            new()
            {
                TaskQueue = network.Type.ToString(),
                StartToCloseTimeout = TimeSpan.FromSeconds(20),
                ScheduleToCloseTimeout = TimeSpan.FromMinutes(20),
                RetryPolicy = new()
                {
                    InitialInterval = TimeSpan.FromSeconds(10),
                    BackoffCoefficient = 1f,
                    MaximumAttempts = 3,
                }
            });

        foreach (var commitMessage in result.HTLCCommitEventMessages)
        {
            if (!_processedTransacrtionHashes.Add(commitMessage.TxId))
            {
                continue;
            }

            await ExecuteActivityAsync(
                (IWorkflowActivities x) => x.StartSwapWorkflowAsync(commitMessage),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }

        foreach (var lockMessage in result.HTLCLockEventMessages)
        {
            if (!_processedTransacrtionHashes.Add(lockMessage.TxId))
            {
                continue;
            }

            try
            {
                await GetExternalWorkflowHandle<ISwapWorkflow>(lockMessage.CommitId)
                    .SignalAsync((x) => x.LockCommitedAsync(lockMessage));
            }
            catch
            {
            }
        }
    }

    [WorkflowQuery]
    public ulong? GetLastScannedBlock()
    {
        return _lastProcessedBlockNumber;
    }

    [WorkflowQuery]
    public HashSet<string> ProcessedTransactionHashes()
    {
        return _processedTransacrtionHashes;
    }
}
