using Temporalio.Workflows;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Common.Helpers;

public static class TemporalHelper
{
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
                return type.ToString();
            case NetworkType.Starknet:
                return $"{type}JS"; // Todo: temp workaround until starknet integration moves to JS
            default:
                throw new("Unsupported network type");
        }
    }

}
