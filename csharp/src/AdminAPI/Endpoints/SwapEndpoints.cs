using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class SwapEndpoints
{
    public static RouteGroupBuilder MapSwapEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/swaps/{commitId}", GetAsync)
            .Produces<SwapDto>();

        return group;
    }

    private static async Task<IResult> GetAsync(
        string commitId,
        ISwapRepository repository)
    {
        var swap = await repository.GetAsync(commitId);

        if (swap == null)
        {
            return Results.NotFound("Swap not found");
        }

        return Results.Ok(swap.ToDto());
    }
}