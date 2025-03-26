using Temporalio.Workflows;
using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Workflows.Helpers;

public static class TemporalHelper
{
    public static ActivityOptions DefaultActivityOptions(NetworkType networkType) =>
        DefaultActivityOptions(networkType.ToString());

    public static ActivityOptions DefaultActivityOptions(string taskQueue) =>
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

    public static string BuildId(string networkName, TransactionType type, Guid uniqueId) => $"{networkName}-{type}-{uniqueId}";
}
