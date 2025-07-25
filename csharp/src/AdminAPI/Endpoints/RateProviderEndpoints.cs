using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class RateProviderEndpoints
{
    public static RouteGroupBuilder MapRateProviderEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rate-providers", GetAllRateProvidersAsync)
            .Produces<List<RateProviderDto>>();

        return group;
    }

    private static async Task<IResult> GetAllRateProvidersAsync(IRouteRepository repository)
    {
        var providers = await repository.GetAllRateProvidersAsync();
        return Results.Ok(providers.Select(x=>x.ToDto()));
    }
}