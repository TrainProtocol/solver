using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class RouteEndpoints
{
    public static RouteGroupBuilder MapRouteEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/routes", GetAllRoutesAsync)
            .Produces<List<RouteDetailedDto>>();

        group.MapPost("/routes", CreateRouteAsync)
            .Produces<RouteDetailedDto>()
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetAllRoutesAsync(
        IRouteRepository repository,
        [FromQuery] RouteStatus[] statuses)
    {
        var routes = await repository.GetAllAsync(statuses);
        return Results.Ok(routes.Select(x=>x.ToDetailedDto()));
    }

    private static async Task<IResult> CreateRouteAsync(
        IRouteRepository repository,
        [FromBody] CreateRouteRequest request)
    {
        var route = await repository.CreateAsync(
            request.SourceNetworkName,
            request.SourceToken,
            request.SourceWalletAddress,
            request.SourceWalletType,
            request.DestinationNetworkName,
            request.DestinationToken,
            request.DestinationWalletAddress,
            request.DestinationWalletType,
            request.RateProvider,
            request.MinAmount,
            request.MaxAmount,
            request.ServiceFee);

        return route is null
            ? Results.BadRequest("Failed to create route")
            : Results.Ok();
    }
}