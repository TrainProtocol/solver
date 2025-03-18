using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Activities;
using Train.Solver.WorkflowRunner.Models;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

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
                    blockBatchSize,
                    waitInterval,
                    _lastProcessedBlockNumber));
            }

            try
            {
                var blockNumberWithHash = await ExecuteActivityAsync(
                    (BlockchainActivities x) => x.GetLastConfirmedBlockNumberAsync(networkName),
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
                   (SwapActivities x) => x.GenerateBlockRanges(
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
                        await Task.WhenAll(blockChunk.Select(x => ProcessBlockRangeAsync(networkName, x)));

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

    private async Task ProcessBlockRangeAsync(string networkName, BlockRange blockRange)
    {
        var result = await ExecuteActivityAsync(
            (BlockchainActivities x) => x.GetEventsAsync(networkName, blockRange.From, blockRange.To),
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
                (SwapActivities x) => x.StartSwapWorkflowAsync(commitMessage),
                Constants.DefaultActivityOptions);
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
