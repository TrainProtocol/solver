using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class NetworkTypeEndpoints
{
    public static RouteGroupBuilder MapNetworkTypeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/network-types", GetAllAsync)
            .Produces<List<string>>();

        return group;
    }

    private static async Task<IResult> GetAllAsync()
    {
        return Results.Ok(typeof(NetworkType).GetEnumNames());
    }
}