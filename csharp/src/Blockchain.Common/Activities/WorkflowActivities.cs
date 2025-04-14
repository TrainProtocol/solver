using System.Text.Json;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Blockchain.Common.Activities;

public class WorkflowActivities(ISwapRepository swapRepository, ITemporalClient temporalClient) : IWorkflowActivities
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
        int waitInterval)
    {
        await temporalClient.StartWorkflowAsync(
            (IEventListenerWorkflow x) =>
                x.RunAsync(
                    networkName,
                    networkType,
                    blockBatchSize,
                    waitInterval,
                    null),
            new(id: TemporalHelper.BuildEventListenerId(networkName),
            taskQueue: networkType.ToString())
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning
            });
    }

    [Activity]
    public async Task StartRefundWorkflowAsync(string swapId)
    {
        var swap = await swapRepository.GetAsync(swapId);

        if (swap == null)
        {
            throw new ArgumentException("Swap not found", nameof(swapId));
        }

        await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(swap.DestinationToken.Network.Type), [new TransactionRequest()
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
            new(id: TemporalHelper.BuildProcessorId(swap.DestinationToken.Network.Name, TransactionType.HTLCRefund, Guid.NewGuid()), taskQueue: swap.DestinationToken.Network.Type.ToString())
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

            var workflowHandle = await temporalClient.StartWorkflowAsync<ISwapWorkflow>(
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
