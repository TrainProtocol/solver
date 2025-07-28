using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.AdminAPI.Endpoints;

public static class NetworkEndpoints
{
    public static RouteGroupBuilder MapNetworkEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/networks", GetAllAsync)
            .Produces<IEnumerable<DetailedNetworkDto>>();

        group.MapGet("/networks/{networkName}", GetAsync)
            .Produces<DetailedNetworkDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/networks", CreateAsync)
            .Produces<DetailedNetworkDto>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/networks/{networkName}/nodes", CreateNodeAsync)
            .Produces<NodeDto>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/networks/{networkName}/nodes/{providerName}", DeleteNodeAsync)
          .Produces<NodeDto>()
          .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/networks/{networkName}/tokens", CreateTokenAsync)
            .Produces<TokenDto>()
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> GetAllAsync(INetworkRepository repository)
    {
        var networks = await repository.GetAllAsync();
        return Results.Ok(networks.Select(x=>x.ToDetailedDto()));
    }

    private static async Task<IResult> GetAsync(
        INetworkRepository repository,
        string networkName)
    {
        var network = await repository.GetAsync(networkName);
        return network is null
            ? Results.NotFound($"Network '{networkName}' not found.")
            : Results.Ok(network.ToDetailedDto());
    }

    private static async Task<IResult> CreateAsync(
        INetworkRepository repository,
        [FromBody] CreateNetworkRequest request)
    {
        var network = await repository.CreateAsync(
            request.NetworkName,
            request.DisplayName,
            request.Type,
            request.FeeType,
            request.ChainId,
            request.FeePercentageIncrease,
            request.HtlcNativeContractAddress,
            request.HtlcTokenContractAddress,
            request.NativeTokenSymbol,
            request.NativeTokenContract,
            request.NativeTokenDecimals);

        return Results.Ok(network.ToDetailedDto());
    }

    private static async Task<IResult> CreateNodeAsync(
        INetworkRepository repository,
        string networkName,
        [FromBody] CreateNodeRequest request)
    {
        var node = await repository.CreateNodeAsync(
            networkName, request.ProviderName, request.Url);

        return Results.Ok(node.ToDto());
    }

    private static async Task<IResult> DeleteNodeAsync(
        INetworkRepository repository,
        string networkName,
        string providerName)
    {
        await repository.DeleteNodeAsync(networkName, providerName);
        return Results.Ok();
    }

    private static async Task<IResult> CreateTokenAsync(
        INetworkRepository repository,
        string networkName,
        [FromBody] CreateTokenRequest request)
    {
        var token = await repository.CreateTokenAsync(
            networkName,
            request.Symbol,
            request.Contract,
            request.Decimals);

        return Results.Ok(token.ToDto());
    }
}