using Temporalio.Workflows;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows.Worklows;

[Workflow]
public class EventListenerWorkflow
{
    private ulong? _lastProcessedBlockNumber;
    private const int _maxConcurrentTaskCount = 4;
    private const int _maxIterationsBeforeContinueAsNew = 200;
    private readonly HashSet<string> _processedTransacrtionHashes = [];

    [WorkflowRun]
    public async Task RunAsync(
        string networkName,
        NetworkType networkType,
        uint blockBatchSize,
        TimeSpan waitInterval,
        ulong? lastProcessedBlockNumber = null)
    {
        _lastProcessedBlockNumber = lastProcessedBlockNumber;

        var iteration = 0;

        while (!Workflow.CancellationToken.IsCancellationRequested)
        {
            // Reset workflow history if it has been running for too long
            if (iteration >= _maxIterationsBeforeContinueAsNew)
            {
                throw CreateContinueAsNewException<EventListenerWorkflow>((x) => x.RunAsync(
                    networkName,
                    networkType,
                    blockBatchSize,
                    waitInterval,
                    _lastProcessedBlockNumber));
            }

            try
            {
                var blockNumberWithHash = await ExecuteActivityAsync<BlockNumberResponse>(
                    $"{networkType}{nameof(IBlockchainActivities.GetLastConfirmedBlockNumberAsync)}",
                    [new BaseRequest { NetworkName = networkName }],
                    new()
                    {
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
                    await DelayAsync(waitInterval);
                    iteration++;
                    continue;
                }

                var blockRanges = await ExecuteLocalActivityAsync(
                   (UtilityActivities x) => x.GenerateBlockRanges(
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
                        await Task.WhenAll(blockChunk.Select(x => ProcessBlockRangeAsync(networkName, networkType, x)));

                        _lastProcessedBlockNumber = blockChunk.Last().To;
                    }

                }
            }
            catch (Exception)
            {
                throw CreateContinueAsNewException<EventListenerWorkflow>((x) => x.RunAsync(
                    networkName,
                    networkType,
                    blockBatchSize,
                    waitInterval,
                    _lastProcessedBlockNumber));
            }

            iteration++;
        }
    }

    private async Task ProcessBlockRangeAsync(
        string networkName,
        NetworkType networkType,
        BlockRangeModel blockRange)
    {
        var result = await ExecuteActivityAsync<HTLCBlockEventResponse>($"{networkType}{nameof(IBlockchainActivities.GetEventsAsync)}",
            [networkName, blockRange.From, blockRange.To],
            new()
            {
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
                (WorkflowActivities x) => x.StartSwapWorkflowAsync(commitMessage),
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
                await GetExternalWorkflowHandle<SwapWorkflow>(lockMessage.Id)
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

    public static string BuildWorkflowId(string networkName)
        => $"{nameof(EventListenerWorkflow)}-{networkName.ToUpper()}";
}
