using System.Text.Json;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Workflows.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Common.Enums;
using Train.Solver.Workflows.Abstractions.Activities;
using Train.Solver.Workflows.Abstractions.Workflows;
using Train.Solver.Workflows.Common.Helpers;
using Train.Solver.Workflows.Common;

namespace Train.Solver.Workflows.Swap.Activities;

public class WorkflowActivities(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
    IWalletRepository walletRepository,
    ITemporalClient temporalClient) : IWorkflowActivities
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
        uint blockBatchSize,
        int waitInterval)
    {
        await temporalClient.StartWorkflowAsync(
            (IEventListenerWorkflow x) =>
                x.RunAsync(
                    networkName,
                    blockBatchSize,
                    waitInterval,
                    null),
            new(id: TemporalHelper.BuildEventListenerId(networkName),
            taskQueue: Constants.CoreTaskQueue)
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

        var wallet = await walletRepository.GetDefaultAsync(swap.DestinationToken.Network.Type);

        if (wallet == null)
        {
            throw new InvalidOperationException($"Solver account not found for network: {swap.DestinationToken.Network.Name}");
        }

        var destinationNetwork = await networkRepository.GetAsync(swap.DestinationToken.Network.Name);

        if (destinationNetwork == null)
        {
            throw new InvalidOperationException($"Network not found: {swap.DestinationToken.Network.Name}");
        }

        await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(swap.DestinationToken.Network.Type), [new TransactionRequest()
                {
                    PrepareArgs = JsonSerializer.Serialize(new HTLCRefundTransactionPrepareRequest
                    {
                        Id = swap.CommitId,
                        Asset = swap.DestinationToken.Asset,
                    }),
                    Type = TransactionType.HTLCRefund,
                    Network = destinationNetwork.ToDetailedDto(),
                    FromAddress = wallet.Address,
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
                return existingSwap.CommitId;
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
