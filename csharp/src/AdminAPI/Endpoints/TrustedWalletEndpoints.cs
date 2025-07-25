using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class TrustedWalletEndpoints
{
    public static RouteGroupBuilder MapTrustedWalletEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/trusted-wallets", GetAllAsync)
            .Produces<IEnumerable<TrustedWallet>>();

        group.MapPost("/trusted-wallets", CreateAsync)
            .Produces<TrustedWalletDto>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/trusted-wallets/{networkType}/{address}", UpdateAsync)
            .Produces<TrustedWalletDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/trusted-wallets/{networkType}/{address}", DeleteAsync)
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        ITrustedWalletRepository repository,
        NetworkType[]? types)
    {
        var wallets = await repository.GetAllAsync(types);
        return Results.Ok(wallets.Select(x=>x.ToDto()));
    }

    private static async Task<IResult> CreateAsync(
        ITrustedWalletRepository repository,
        [FromBody] CreateTrustedWalletRequest request)
    {
        var wallet = await repository.CreateAsync(
            request.NetworkType,
            request.Address,
            request.Name);

        return wallet is null
            ? Results.BadRequest("Failed to create trusted wallet")
            : Results.Ok(wallet.ToDto());
    }

    private static async Task<IResult> UpdateAsync(
        ITrustedWalletRepository repository,
        NetworkType networkType,
        string address,
        [FromBody] UpdateTrustedWalletRequest request)
    {
        var wallet = await repository.UpdateAsync(
            networkType,
            address,
            request.Name);

        return wallet is null
            ? Results.NotFound($"Trusted wallet '{address}' not found on network '{networkType}'")
            : Results.Ok(wallet.ToDto());
    }

    private static async Task<IResult> DeleteAsync(
        ITrustedWalletRepository repository,
        NetworkType networkType,
        string address)
    {
        await repository.DeleteAsync(networkType, address);
        return Results.NoContent();
    }
}