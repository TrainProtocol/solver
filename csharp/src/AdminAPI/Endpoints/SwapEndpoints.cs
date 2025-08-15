using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;
using Temporalio.Client;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;

namespace Train.Solver.AdminAPI.Endpoints;

public static class SwapEndpoints
{
    public static RouteGroupBuilder MapSwapEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/swaps", GetAllAsync)
            .Produces<List<SwapDto>>();

        group.MapGet("/swaps/{commitId}", GetAsync)
            .Produces<SwapDto>();

        group.MapPost("/swaps/{commitId}/refund", RefundAsync)
           .Produces(200);

        group.MapGet("/swaps/pending-refund", GetPendingRefundAsync)
            .Produces<List<string>>();

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        ISwapRepository repository,
        uint page = 1)
    {
        var swaps = await repository.GetAllAsync(page);

        return Results.Ok(swaps.Select(x => x.ToDto()));
    }
    private static async Task<IResult> GetPendingRefundAsync(
        ISwapRepository repository)
    {
        var swapCommitIds = await repository.GetNonRefundedSwapsAsync();

        return Results.Ok(swapCommitIds);
    }

    private static async Task<IResult> GetAsync(
        [FromRoute] string commitId,
        ISwapRepository repository)
    {
        var swap = await repository.GetAsync(commitId);

        if (swap == null)
        {
            return Results.NotFound("Swap not found");
        }

        return Results.Ok(swap.ToDto());
    }

    private static async Task<IResult> RefundAsync(
       ISwapRepository repository,
       IWalletRepository walletRepository,
       ITemporalClient temporalClient,
       [FromRoute] string commitId,
       [FromBody] RefundRequest request)
    {
        var swap = await repository.GetAsync(commitId);

        if (swap == null)
        {
            return Results.NotFound("Swap not found");
        }

        if (swap.Transactions.Any(t => t.Type == TransactionType.HTLCRefund))
        {
            return Results.BadRequest("Swap already refunded");
        }

        var wallet = await walletRepository.GetAsync(request.Type, request.Address);

        if (wallet == null)
        {
            return Results.BadRequest("Wallet not found");
        }

        if (wallet.NetworkType != swap.Route.DestinationToken.Network.Type)
        {
            return Results.BadRequest("Wallet network type does not match swap destination network type");
        }

        var workflowId = await temporalClient.StartWorkflowAsync(
            (IRefundWorkflow w) => w.RunAsync(commitId, request.Address, wallet.SignerAgent.Name),
                 new(id: TemporalHelper.BuildRefundId(commitId), taskQueue: Constants.CoreTaskQueue));

        return Results.Ok();
    }
}