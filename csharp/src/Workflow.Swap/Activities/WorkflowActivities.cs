using System.Text.Json;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Common.Enums;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Common;
using Train.Solver.Common.Extensions;

namespace Train.Solver.Workflow.Swap.Activities;

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

        var destinationNetwork = await networkRepository.GetAsync(swap.Route.DestinationToken.Network.Name);

        if (destinationNetwork == null)
        {
            throw new ArgumentException($"Destination network {swap.Route.DestinationToken.Network.Name} not found", nameof(swapId));
        }

        if (destinationNetwork.Type == NetworkType.Fuel)
        {


            await temporalClient.StartWorkflowAsync(
                TemporalHelper.ResolveProcessor(swap.Route.DestinationToken.Network.Type), [new TransactionRequest()
                {
                    PrepareArgs = new HTLCRefundTransactionPrepareRequest
                    {
                        CommitId = swap.CommitId,
                        Asset = swap.Route.DestinationToken.Asset,
                    }.ToJson(),
                    Type = TransactionType.HTLCRefund,
                    Network = destinationNetwork.ToDetailedDto(),
                    FromAddress = swap.Route.DestinationWallet.Address,
                    SignerAgentUrl = swap.Route.DestinationWallet.SignerAgent.Url,
                    SwapId = swap.Id,
            }, new TransactionExecutionContext()],
                new(id: TemporalHelper.BuildProcessorId(swap.Route.DestinationToken.Network.Name, TransactionType.HTLCRefund, Guid.NewGuid()), taskQueue: swap.Route.DestinationToken.Network.Type.ToString())
                {
                    IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning,
                });
        }




        var sourceNetwork = await networkRepository.GetAsync(swap.Route.SourceToken.Network.Name);

        if (sourceNetwork == null)
        {
            throw new ArgumentException($"Source network {swap.Route.SourceToken.Network.Name} not found", nameof(swapId));
        }

        if (sourceNetwork.Type == NetworkType.Fuel)
        {

            await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(swap.Route.SourceToken.Network.Type), [new TransactionRequest()
                {
                    PrepareArgs = new HTLCRefundTransactionPrepareRequest
                    {
                        CommitId = swap.CommitId,
                        Asset = swap.Route.SourceToken.Asset,
                    }.ToJson(),
                    Type = TransactionType.HTLCRefund,
                    Network = sourceNetwork.ToDetailedDto(),
                    FromAddress = swap.Route.SourceWallet.Address,
                    SignerAgentUrl = swap.Route.SourceWallet.SignerAgent.Url,
                    SwapId = swap.Id,
            }, new TransactionExecutionContext()],
            new(id: TemporalHelper.BuildProcessorId(swap.Route.SourceToken.Network.Name, TransactionType.HTLCRefund, Guid.NewGuid()), taskQueue: swap.Route.SourceToken.Network.Type.ToString())
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning,
            });
        }
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
            var existingSwap = await swapRepository.GetAsync(signal.CommitId);

            if (existingSwap != null)
            {
                return existingSwap.CommitId;
            }

            var workflowHandle = await temporalClient.StartWorkflowAsync<ISwapWorkflow>(
                workflow => workflow.RunAsync(signal),
                new WorkflowOptions
                {
                    Id = $"{signal.CommitId}",
                    TaskQueue = Constants.CoreTaskQueue,
                });

            return workflowHandle.Id;
        }
        catch (WorkflowAlreadyStartedException)
        {
            return signal.CommitId;
        }
    }
}
