using System.Reflection;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchains.EVM.Workflows;
using Train.Solver.Core.Blockchains.Solana.Workflows;
using Train.Solver.Core.Blockchains.Starknet.Workflows;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Helpers;

public static class TemporalHelper
{
    public static ActivityOptions DefaultActivityOptions(NetworkGroup networkGroup) =>
        DefaultActivityOptions(ResolveTaskQueue(networkGroup));

    public static ActivityOptions DefaultActivityOptions(string taskQueue) =>
    new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
        StartToCloseTimeout = TimeSpan.FromHours(1),
        TaskQueue = taskQueue
    };

    public static string ResolveProcessor(NetworkGroup networkGroup)
    {
        Type type = networkGroup switch
        {
            // Networks starting with EVMTransactionProcessorWorkflow
            NetworkGroup.EVMEthereumLegacy or
            NetworkGroup.EVMEthereumEip1559 or
            NetworkGroup.EVMArbitrumLegacy or
            NetworkGroup.EVMArbitrumEip1559 or
            NetworkGroup.EVMOptimismEip1559 or
            NetworkGroup.EVMOptimismLegacy or
            NetworkGroup.EVMPolygonLegacy or
            NetworkGroup.EVMPolygonEip1559 => typeof(EVMTransactionProcessor),

            // Networks starting with Starknet
            NetworkGroup.Starknet => typeof(StarknetTransactionProcessor),

            // SolanaTransactionProcessorWorkflow-specific network
            NetworkGroup.Solana => typeof(SolanaTransactionProcessor),

            // Default for unimplemented networks
            _ => throw new NotImplementedException($"No processor implemented for {networkGroup}")
        };

        var workflowAttribute = type.GetCustomAttribute<WorkflowAttribute>();

        if (workflowAttribute == null)
        {
            throw new InvalidOperationException($"Type {type.Name} does not have a Workflow attribute.");
        }

        return string.IsNullOrEmpty(workflowAttribute.Name)
            ? type.Name
            : workflowAttribute.Name;
    }

    public static string ResolveTaskQueue(NetworkGroup networkGroup)
    {
        return networkGroup switch
        {
            NetworkGroup.EVMEthereumLegacy or
            NetworkGroup.EVMEthereumEip1559 or
            NetworkGroup.EVMArbitrumLegacy or
            NetworkGroup.EVMArbitrumEip1559 or
            NetworkGroup.EVMOptimismEip1559 or
            NetworkGroup.EVMOptimismLegacy or
            NetworkGroup.EVMPolygonLegacy or
            NetworkGroup.EVMPolygonEip1559 or
            NetworkGroup.Solana => Constants.CSharpTaskQueue,
            NetworkGroup.Starknet => Constants.JsTaskQueue,
            _ => throw new NotImplementedException($"No processor implemented for {networkGroup}")
        };
    }
}
