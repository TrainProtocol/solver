using System.Linq.Expressions;
using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Util;

namespace Train.Solver.Blockchain.Common.Helpers;

public static class TemporalHelper
{
    public static async Task<TResult> ExecuteChildTransactionProcessorWorkflowAsync<TResult>(
       NetworkType networkType,
       Expression<Func<ITransactionProcessor, Task<TResult>>> workflowRunCall,
       ChildWorkflowOptions? options = null)
    {
        var (_, args) = ExpressionUtil.ExtractCall(workflowRunCall);
        var handle = await Workflow.StartChildWorkflowAsync(
            ResolveProcessor(networkType),
            args,
            options ?? new()).ConfigureAwait(true);

        return await handle.GetResultAsync<TResult>().ConfigureAwait(true);
    }

    public static ActivityOptions DefaultActivityOptions() =>
        DefaultActivityOptions(null);

    public static ActivityOptions DefaultActivityOptions(NetworkType networkType) =>
        DefaultActivityOptions(networkType.ToString());

    public static ActivityOptions DefaultActivityOptions(string? taskQueue) =>
        new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromDays(2),
            StartToCloseTimeout = TimeSpan.FromHours(1),
            TaskQueue = taskQueue
        };

    public static string ResolveProcessor(NetworkType networkType)
    {
        return $"{networkType}TransactionProcessor";
    }

    public static string BuildEventListenerId(string networkName)
        => $"{nameof(EventListenerWorkflow)}-{networkName.ToUpper()}";

    public static string BuildProcessorId(string networkName, TransactionType type, Guid uniqueId) => $"{networkName}-{type}-{uniqueId}";

    public static string ResolveBlockchainActivityTaskQueue(NetworkType type)
    {
        switch (type)
        {
            case NetworkType.EVM:
            case NetworkType.Solana:
            case NetworkType.Starknet:
                return type.ToString();
            default:
                throw new("Unsupported network type");
        }
    }

}
