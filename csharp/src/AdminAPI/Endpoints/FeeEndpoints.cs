using Microsoft.AspNetCore.Mvc;
using Train.Solver.Data.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class FeeEndpoints
{
    public static RouteGroupBuilder MapFeeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/fees", GetServiceFeesAsync)
            .Produces<List<ServiceFeeDto>>();

        group.MapPost("/fees", CreateServiceFeeAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/fees/{name}", UpdateServiceFeeAsync)
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetServiceFeesAsync(IFeeRepository repository)
    {
        var fees = await repository.GetServiceFeesAsync();
        return Results.Ok(fees.Select(x => x.ToDto()));
    }

    private static async Task<IResult> CreateServiceFeeAsync(
        IFeeRepository repository,
        [FromBody] CreateServiceFeeRequest request)
    {
        var fee = await repository.CreateServiceFeeAsync(request);
        return fee is null
            ? Results.BadRequest("Failed to create service fee")
            : Results.Ok();
    }

    private static async Task<IResult> UpdateServiceFeeAsync(
      IFeeRepository repository,
      string name,
      [FromBody] UpdateServiceFeeRequest request)
    {
        var fee = await repository.UpdateServiceFeeAsync(name, request);
      
        return fee is null
            ? Results.BadRequest("Failed to create service fee")
            : Results.Ok();
    }
}