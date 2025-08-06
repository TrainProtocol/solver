using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class SignerAgentEndpoints
{
    public static RouteGroupBuilder MapSignerAgentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/signer-agents", GetAllAsync)
            .Produces<IEnumerable<TrustedWalletDto>>();

        group.MapPost("/signer-agents", CreateAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        ISignerAgentRepository repository)
    {
        var signerAgents = await repository.GetAllAsync();
        return Results.Ok(signerAgents.Select(x => x.ToDto()));
    }

    private static async Task<IResult> CreateAsync(
        ISignerAgentRepository repository,
        [FromBody] CreateSignerAgentRequest request)
    {
        var signerAgent = await repository.CreateAsync(
            request.Name,
            request.Url,
            request.SupportedTypes);

        return signerAgent is null
            ? Results.BadRequest("Failed to create signer wallet")
            : Results.Ok();
    }
}