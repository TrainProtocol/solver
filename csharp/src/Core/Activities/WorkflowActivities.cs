using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Core.Data;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Workflows;

namespace Train.Solver.Core.Activities;

public class WorkflowActivities(SolverDbContext dbContext, ITemporalClient temporalClient)
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
        NetworkGroup networkGroup,
        uint blockBatchSize,
        TimeSpan waitInterval)
    {
        await temporalClient.StartWorkflowAsync(
            (EventListenerWorkflow x) =>
                x.RunAsync(
                    networkName,
                    networkGroup,
                    blockBatchSize,
                    waitInterval,
                    null),
            new(id: EventListenerWorkflow.BuildWorkflowId(networkName),
            taskQueue: Constants.CSharpTaskQueue)
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning
            });
    }

    [Activity]
    public async Task StartRefundWorkflowAsync(string swapId)
    {
        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .Include(x => x.DestinationToken.Network)
                .ThenInclude(network => network.ManagedAccounts)
            .SingleAsync(s => s.Id == swapId);

        await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(swap.DestinationToken.Network.Group), [new TransactionContext()
                {
                    PrepareArgs = JsonSerializer.Serialize(new HTLCRefundTransactionPrepareRequest
                    {
                        Id = swap.Id,
                        Asset = swap.DestinationToken.Asset,
                    }),
                    Type = TransactionType.HTLCRefund,
                    NetworkName = swap.DestinationToken.Network.Name,
                    NetworkGroup = swap.DestinationToken.Network.Group,
                    FromAddress = swap.DestinationToken.Network.ManagedAccounts.First().Address,
                    SwapId = swap.Id,
            }],
            new(id: TransactionProcessorBase.BuildId(swap.DestinationToken.Network.Name, TransactionType.HTLCRefund), taskQueue: Constants.CSharpTaskQueue)
            {
                IdReusePolicy = Temporalio.Api.Enums.V1.WorkflowIdReusePolicy.TerminateIfRunning,
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
            var existingSwap = await dbContext.Swaps
                .SingleOrDefaultAsync(x => x.Id == signal.Id);

            if (existingSwap != null)
            {
                return existingSwap.Id;
            }

            var workflowHandle = await temporalClient.StartWorkflowAsync<SwapWorkflow>(
                workflow => workflow.RunAsync(signal),
                new WorkflowOptions
                {
                    Id = $"{signal.Id}",
                    TaskQueue = Constants.CSharpTaskQueue,
                });

            return workflowHandle.Id;
        }
        catch (WorkflowAlreadyStartedException)
        {
            return signal.Id;
        }
    }
}
