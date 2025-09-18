using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;
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

        group.MapPut("/routes/{sourceNetwork}/{sourceToken}/{destinationNetwork}/{destinationToken}", UpdateRouteAsync)
           .Produces<RouteDetailedDto>()
           .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetAllRoutesAsync(
        IRouteRepository repository,
        [FromQuery] RouteStatus[]? statuses)
    {
        var routes = await repository.GetAllAsync(statuses.IsNullOrEmpty() ? null : statuses);
        return Results.Ok(routes.Select(x=>x.ToDetailedDto()));
    }

    private static async Task<IResult> CreateRouteAsync(
        IRouteRepository repository,
        [FromBody] CreateRouteRequest request)
    {
        var route = await repository.CreateAsync(
            request);

        return route is null
            ? Results.BadRequest("Failed to create route")
            : Results.Ok();
    }

    private static async Task<IResult> UpdateRouteAsync(
        IRouteRepository repository,
        string sourceNetwork,
        string sourceToken,
        string destinationNetwork,
        string destinationToken,
        [FromBody] UpdateRouteRequest request)
    {
        var route = await repository.UpdateAsync(
            sourceNetwork,
            sourceToken,
            destinationNetwork,
            destinationToken,
            request);

        return route is null
            ? Results.BadRequest("Failed to update route")
            : Results.Ok();
    }
}