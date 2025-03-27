using Temporalio.Workflows;
using Train.Solver.Core.Abstractions.Entities;

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

    public static string ResolveBlockchainActivityTaskQueue(NetworkType type)
    {
        switch (type)
        {
            case NetworkType.EVM:
            case NetworkType.Solana:
                return type.ToString();
            case NetworkType.Starknet:
                return $"{type}JS"; // Todo: temp workaround until starknet integration moves to JS
            default:
                throw new("Unsupported network type");
        }
    }

    public static string BuildId(string networkName, TransactionType type, Guid uniqueId) => $"{networkName}-{type}-{uniqueId}";
}
