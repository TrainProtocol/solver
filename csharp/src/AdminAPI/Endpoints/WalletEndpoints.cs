using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class WalletEndpoints
{
    public static RouteGroupBuilder MapWalletEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/wallets", GetAllAsync)
            .Produces<IEnumerable<WalletDto>>();

        group.MapPost("/wallets", CreateAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/wallets/{networkType}/{address}", UpdateAsync)
            .Produces(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        IWalletRepository repository,
        [FromQuery] NetworkType[]? types)
    {
        var wallets = await repository.GetAllAsync(types.IsNullOrEmpty() ? null : types);
        return Results.Ok(wallets.Select(x=> x.ToDto()));
    }

    private static async Task<IResult> CreateAsync(
        IWalletRepository repository,
        IPrivateKeyProvider privateKeyProvider,
        [FromBody] CreateWalletRequest request)
    {
        var generatedAddress = await privateKeyProvider.GenerateAsync(request.NetworkType);

        var wallet = await repository.CreateAsync(request.NetworkType, generatedAddress, request.Name);
        return wallet is null
            ? Results.BadRequest("Could not create wallet")
            : Results.Ok();
    }

    private static async Task<IResult> UpdateAsync(
      IWalletRepository repository,
      NetworkType networkType,
      string address,
      [FromBody] UpdateWalletRequest request)
    {
        var wallet = await repository.UpdateAsync(
            networkType,
            address,
            request.Name);

        return wallet is null
            ? Results.NotFound($"Trusted wallet '{address}' not found on network '{networkType}'")
            : Results.Ok();
    }
}