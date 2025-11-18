using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class TokenPriceEndpoints
{
    public static RouteGroupBuilder MapTokenPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/token-prices", GetAllTokenPricesAsync)
            .Produces<List<TokenPriceDto>>();

        group.MapPost("/token-prices", CreateTokenPriceAsync)
           .Produces(200);

        return group;
    }

    private static async Task<IResult> GetAllTokenPricesAsync(ITokenPriceRepository repository)
    {
        var tokenPrices = await repository.GetAllAsync();
        return Results.Ok(tokenPrices.Select(x => x.ToDto()));
    }

    private static async Task<IResult> CreateTokenPriceAsync(
        ITokenPriceRepository repository,
        CreateTokenPriceRequest request)
    {
        var tokenPrice = await repository.CreateAsync(request);

        return tokenPrice is null
           ? Results.BadRequest("Failed to create token price")
           : Results.Ok();
    }
}