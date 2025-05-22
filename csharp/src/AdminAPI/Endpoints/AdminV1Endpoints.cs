using Microsoft.AspNetCore.Mvc;
using Temporalio.Client;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.AdminAPI.Models;
using Temporalio.Api.Enums.V1;
using Train.Solver.Blockchain.Common.Helpers;
using System.Text.Json;

namespace Train.Solver.AdminAPI.Endpoints;

public static class AdminV1Endpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rebalance", RebalanceAsync)
            .Produces<string>();

        return group;
    }

    private async static Task<IResult> RebalanceAsync(
        INetworkRepository networkRepository,
        ITemporalClient temporalClient,
        [FromBody] RebalanceRequest rebalanceRequest)
    {
        var network = await networkRepository.GetAsync(rebalanceRequest.NetworkName);

        if (network == null)
        {
            return Results.NotFound($"Network {rebalanceRequest.NetworkName} not found");
        }

        var token = network.Tokens.FirstOrDefault(x => x.Asset == rebalanceRequest.Token);

        if (token == null)
        {
            return Results.NotFound($"Token {rebalanceRequest.Token} not found in network {rebalanceRequest.NetworkName}");
        }

        var fromAccount = network.ManagedAccounts
            .FirstOrDefault(x => x.Type == rebalanceRequest.From);

        if (fromAccount == null)
        {
            return Results.NotFound($"From account {rebalanceRequest.From} not found in network {rebalanceRequest.NetworkName}");
        }

        var toAccount = network.ManagedAccounts
            .FirstOrDefault(x => x.Type == rebalanceRequest.To);

        if (toAccount == null)
        {
            return Results.NotFound($"To account {rebalanceRequest.To} not found in network {rebalanceRequest.NetworkName}");
        }

        var workflowId =  await temporalClient.StartWorkflowAsync(
            TemporalHelper.ResolveProcessor(network.Type), [new TransactionRequest()
                {
                    PrepareArgs = JsonSerializer.Serialize(new TransferPrepareRequest
                    {
                      Amount = rebalanceRequest.Amount,
                      Asset = token.Asset,
                      FromAddress = fromAccount.Address,
                      ToAddress = toAccount.Address,
                    }),
                    Type = TransactionType.Transfer,
                    NetworkName = network.Name,
                    NetworkType = network.Type,
                    FromAddress = fromAccount.Address,
            },
            new TransactionExecutionContext()],
            new(id: TemporalHelper.BuildProcessorId(network.Name, TransactionType.Transfer, Guid.NewGuid()),
            taskQueue: network.Type.ToString())
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning,
            });

        return Results.Ok();
    }
}
