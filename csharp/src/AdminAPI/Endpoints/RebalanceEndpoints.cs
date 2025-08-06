using Microsoft.AspNetCore.Mvc;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Common.Helpers;

namespace Train.Solver.AdminAPI.Endpoints;

public static class RebalanceEndpoints
{
    public static RouteGroupBuilder MapRebalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/rebalance", RebalanceAsync)
            .Produces(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> RebalanceAsync(
        INetworkRepository repository,
        IWalletRepository walletRepository,
        ITrustedWalletRepository trustedWalletRepository,
        ITemporalClient temporalClient,
        [FromBody] RebalanceRequest request)
    {
        var network = await repository.GetAsync(request.NetworkName);

        if (network == null)
        {
            return Results.NotFound($"Network {request.NetworkName} not found");
        }

        var token = network.Tokens
            .FirstOrDefault(x => x.Asset == request.Token);

        if (token is null)
        {
            return Results.NotFound($"Token {request.Token} not found on network {request.NetworkName}");
        }

        var wallet = await walletRepository.GetAsync(network.Type, request.FromAddress);

        if (wallet is null)
        {
            return Results.NotFound($"Wallet {request.FromAddress} not found on network {request.NetworkName}");
        }

        var trustedWallet = await trustedWalletRepository.GetAsync(network.Type, request.ToAddress);

        if (trustedWallet is null)
        {
            return Results.NotFound($"Trusted wallet {request.ToAddress} not found on network {request.NetworkName}");
        }

        var workflowId = await temporalClient.StartWorkflowAsync(
                    TemporalHelper.ResolveProcessor(network.Type), [new TransactionRequest()
                        {
                            PrepareArgs = new TransferPrepareRequest
                            {
                              Amount = request.Amount,
                              Asset = token.Asset,
                              FromAddress = wallet.Address,
                              ToAddress = trustedWallet.Address,
                            }.ToJson(),
                            Type = TransactionType.Transfer,
                            Network = network.ToDetailedDto(),
                            FromAddress = wallet.Address,
                            SignerAgentUrl = wallet.SignerAgent.Url,
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
