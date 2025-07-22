using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
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

        //group.MapGet("/wallets/{networkType}/{address}", GetAsync)
        //    .Produces<WalletDto>()
        //    .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/wallets", CreateAsync)
            .Produces<WalletDto>()
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetAllAsync(IWalletRepository repository)
    {
        var wallets = await repository.GetAllAsync();
        return Results.Ok(wallets.Select(x=> x.ToDto()));
    }

    private static async Task<IResult> GetAsync(
        IWalletRepository repository,
        NetworkType networkType,
        string address)
    {
        var wallet = await repository.GetAsync(networkType, address);
        return wallet is null
            ? Results.NotFound($"Wallet '{address}' not found on network type '{networkType}'")
            : Results.Ok(wallet.ToDto());
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
            : Results.Ok(wallet.ToDto());
    }
}