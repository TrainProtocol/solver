using System.Text.Json;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.Core.Workflows.Worklows;

namespace Train.Solver.Core.Workflows.Activities;

public class WorkflowActivities(ISwapRepository swapRepository, ITemporalClient temporalClient)
{
    [Activity]
    public virtual async Task TerminateWorkflowAsync(string workflowId)
    {
        var handle = temporalClient.GetWorkflowHandle(workflowId);
        await handle.TerminateAsync();
    }

    [Activity]
    public async Task<List<string>> GetRunningWorkflowIdsAsync(string workflowType)
    {
        string query = $"WorkflowType='{workflowType}' AND ExecutionStatus='Running'";
        IAsyncEnumerable<WorkflowExecution> workflows = temporalClient.ListWorkflowsAsync(query);

        var runningWorkflowsIds = new List<string>();
        await foreach (var execution in workflows)
        {
            runningWorkflowsIds.Add(execution.Id);
        }

        return runningWorkflowsIds;
    }

    [Activity]
    public virtual async Task RunEventListeningWorkflowAsync(
        string networkName,
        NetworkType networkType,
        uint blockBatchSize,
        TimeSpan waitInterval)
    {
        await temporalClient.StartWorkflowAsync(
            (EventListenerWorkflow x) =>
                x.RunAsync(
                    networkName,
                    networkType,
                    blockBatchSize,
                    waitInterval,
                    null),
            new(id: EventListenerWorkflow.BuildWorkflowId(networkName),
            taskQueue: networkType.ToString())
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning
            });
    }

    [Activity]
    public async Task StartRefundWorkflowAsync(string swapId)
    {
        var swap = await swapRepository.GetAsync(swapId);

        if(swap == null)
        {
            throw new ArgumentException("Swap not found", nameof(swapId));
        }

        await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(swap.DestinationToken.Network.Type), [new TransactionContext()
                {
                    PrepareArgs = JsonSerializer.Serialize(new HTLCRefundTransactionPrepareRequest
                    {
                        Id = swap.Id,
                        Asset = swap.DestinationToken.Asset,
                    }),
                    Type = TransactionType.HTLCRefund,
                    NetworkName = swap.DestinationToken.Network.Name,
                    NetworkType = swap.DestinationToken.Network.Type,
                    FromAddress = swap.DestinationToken.Network.ManagedAccounts.First().Address,
                    SwapId = swap.Id,
            }],
            new(id: TemporalHelper.BuildId(swap.DestinationToken.Network.Name, TransactionType.HTLCRefund, Guid.NewGuid()), taskQueue: swap.DestinationToken.Network.Type.ToString())
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning,
            });
    }

    [Activity]
    public async Task<string> StartSwapWorkflowAsync(HTLCCommitEventMessage signal)
    {
        if (signal == null)
        {
            throw new ArgumentNullException(nameof(signal), "Signal cannot be null.");
        }

        try
        {
            var existingSwap = await swapRepository.GetAsync(signal.Id);

            if (existingSwap != null)
            {
                return existingSwap.Id;
            }

            var workflowHandle = await temporalClient.StartWorkflowAsync<SwapWorkflow>(
                workflow => workflow.RunAsync(signal),
                new WorkflowOptions
                {
                    Id = $"{signal.Id}",
                    TaskQueue = Constants.CoreTaskQueue,
                });

            return workflowHandle.Id;
        }
        catch (WorkflowAlreadyStartedException)
        {
            return signal.Id;
        }
    }
}
