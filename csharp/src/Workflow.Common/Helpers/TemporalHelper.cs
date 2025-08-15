using System.Linq.Expressions;
using Temporalio.Workflows;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;
using Train.Solver.Workflow.Abstractions.Workflows;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Common.Helpers;

public static class TemporalHelper
{
    public static async Task<TResult> ExecuteChildTransactionProcessorWorkflowAsync<TResult>(
       NetworkType networkType,
       Expression<Func<ITransactionProcessor, Task<TResult>>> workflowRunCall,
       ChildWorkflowOptions? options = null)
    {
        var (_, args) = ExpressionHelper.ExtractCall(workflowRunCall);
        var handle = await StartChildWorkflowAsync(
            ResolveProcessor(networkType),
            args,
            options ?? new()).ConfigureAwait(true);

        return await handle.GetResultAsync<TResult>().ConfigureAwait(true);
    }

    public static ActivityOptions DefaultActivityOptions(string? summary = null) =>
        DefaultActivityOptions(null, summary);

    public static ActivityOptions DefaultActivityOptions(NetworkType networkType, string? summary = null) =>
        DefaultActivityOptions(networkType.ToString(), summary);

    public static ActivityOptions DefaultActivityOptions(string? taskQueue, string? summary = null) =>
        new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromDays(2),
            StartToCloseTimeout = TimeSpan.FromHours(1),
            TaskQueue = taskQueue,
            Summary = summary,
        };

    public static string ResolveProcessor(NetworkType networkType)
    {
        return $"{networkType}TransactionProcessor";
    }

    public static string BuildEventListenerId(string networkName)
        => $"EventListenerWorkflow-{networkName.ToUpper()}";

    public static string BuildProcessorId(string networkName, TransactionType type, Guid uniqueId) => $"{networkName}-{type}-{uniqueId}";

    public static string BuildRebalanceProcessorId(string networkName, Guid uniqueId) 
        => $"Rebalance-{networkName}-{uniqueId}";

    public static string BuildRefundId(string commitId)
        => $"Refund-{commitId}";
}
