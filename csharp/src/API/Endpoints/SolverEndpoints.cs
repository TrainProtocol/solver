using AutoMapper;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Temporalio.Client;
using Train.Solver.API.Extensions;
using Train.Solver.API.Models;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services;
using Train.Solver.Core.Temporal.Abstractions;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.API.Endpoints;

public static class SolverEndpoints
{
    public const int UsdPrecision = 6;

    public static RouteGroupBuilder MapEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/networks", GetNetworksAsync)
            .Produces<ApiResponse<List<NetworkWithTokensModel>>>();

        group.MapGet("/sources", GetAllSourcesAsync)
            .Produces<ApiResponse<List<NetworkWithTokensModel>>>();

        group.MapGet("/destinations", GetAllDestinationsAsync)
            .Produces<ApiResponse<List<NetworkWithTokensModel>>>();

        group.MapGet("/limits", GetSwapRouteLimitsAsync)
          .Produces<ApiResponse<Models.LimitModel>>();

        group.MapGet("/quote", GetQuoteAsync)
            .Produces<ApiResponse<Models.QuoteModel>>();

        group.MapGet("/swaps", GetAllSwapsAsync)
            .Produces<ApiResponse<SwapModel>>();

        group.MapGet("/swaps/{commitId}", GetSwapAsync)
            .Produces<ApiResponse<SwapModel>>();

        group.MapPost("swaps/{commitId}/add_lock_sig", AddLockSigAsync)
            .Produces<ApiResponse>();

        return group;
    }

    private async static Task<IResult> AddLockSigAsync(
        [FromServices] SolverDbContext dbContext,
        [FromServices] ITemporalClient temporalClient,
        IKeyedServiceProvider serviceProvider,
        [FromRoute] string commitId,
        [FromBody] AddLockSigRequest addLockSigRequest)
    {

        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .FirstOrDefaultAsync(x => x.Id == commitId);

        if (swap is null)
        {
            return Result.Fail(new NotFoundError($"Swap not found")).ToHttpResult();
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(swap.SourceToken.Network.Name);

        if (blockchainService != null)
        {
            var validationResult = await blockchainService.ValidateAddLockSignatureAsync(swap.SourceToken.Network.Name, new()
            {
                Id = swap.Id,
                Hashlock = swap.Hashlock,
                Timelock = addLockSigRequest.Timelock,
                R = addLockSigRequest.R,
                S = addLockSigRequest.S,
                V = addLockSigRequest.V,
                SignatureArray = addLockSigRequest.SignatureArray,
                Asset = swap.SourceToken.Asset,
                SignerAddress = swap.SourceAddress,
            });

            if (validationResult.IsFailed)
            {
                return validationResult.ToHttpResult();
            }

            if (!validationResult.Value)
            {
                return Result.Fail(new BadRequestError("Invalid signature")).ToHttpResult();
            }
        }

        var handle = temporalClient.GetWorkflowHandle<ISwapWorkflow>(commitId);


        await handle.SignalAsync((x) => x.AddLockSignatureAsync(addLockSigRequest));

        return Result.Ok().ToHttpResult();
    }

    private async static Task<IResult> GetSwapAsync(
        ITemporalClient temporalClient,
        IMapper mapper,
        SolverDbContext dbContext,
        [FromRoute] string commitId)
    {
        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .Include(x => x.DestinationToken.Network)
            .Include(x => x.Transactions.Where(x => x.Status == TransactionStatus.Completed))
            .FirstOrDefaultAsync(x => x.Id == commitId);

        if (swap is null)
        {
            return Result.Fail(new NotFoundError("Swap not found")).ToHttpResult();
        }

        return Result.Ok(mapper.Map<SwapModel>(swap)).ToHttpResult();
    }

    private async static Task<IResult> GetAllSwapsAsync(
        SolverDbContext dbContext,
        IMapper mapper,
        [FromQuery] string[]? addresses,
        [FromQuery] uint? page)
    {
        var pageSize = 20;
        if (addresses != null && addresses.Length > 6)
        {
            return Result.Fail(new BadRequestError("Too many addresses provided")).ToHttpResult();
        }

        addresses = addresses?.Select(x => x.ToLower()).Distinct().ToArray();

        var swaps = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .Include(x => x.DestinationToken.Network)
            .Include(x => x.Transactions)
            .Where(x => addresses == null
                || addresses.Contains(x.SourceAddress.ToLower())
                || addresses.Contains(x.DestinationAddress.ToLower()))
            .OrderByDescending(x => x.CreatedDate)
            .Skip((int)(page ?? 0) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (!swaps.Any())
        {
            return Result.Ok(Enumerable.Empty<SwapModel>()).ToHttpResult();
        }

        return Result.Ok(mapper.Map<List<SwapModel>>(swaps)).ToHttpResult();
    }

    private async static Task<IResult> GetSwapRouteLimitsAsync(
      IMapper mapper,
      SolverDbContext dbContext,
      HttpContext httpContext,
      RouteService routeService,
      [AsParameters] GetRouteLimitsQueryParams queryParams)
    {
        var limitResult = await routeService.GetLimitAsync(
            new()
            {
                SourceNetwork = queryParams.SourceNetwork!,
                SourceToken = queryParams.SourceAsset!,
                DestinationNetwork = queryParams.DestinationNetwork!,
                DestinationToken = queryParams.DestinationAsset!,
            });

        if (limitResult.IsFailed)
        {
            return limitResult.ToHttpResult();
        }

        var route = limitResult.Value.Route;
        var mappedLimit = mapper.Map<Models.LimitModel>(limitResult.Value);

        mappedLimit.MaxAmountInUsd = (mappedLimit.MaxAmount * route.Source.UsdPrice).Truncate(UsdPrecision);
        mappedLimit.MinAmountInUsd = (mappedLimit.MinAmount * route.Source.UsdPrice).Truncate(UsdPrecision);

        return Result.Ok(mappedLimit).ToHttpResult();
    }

    private async static Task<IResult> GetNetworksAsync(
        HttpContext httpContext,
        SolverDbContext dbContext,
        IMapper mapper)
    {
        var networks = await dbContext.Networks
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes.Where(y => y.Type == NodeType.Public))
            .Include(x => x.ManagedAccounts.Where(y => y.Type == AccountType.LP))
            .Include(x => x.DeployedContracts)
            .ToListAsync();

        var mappedNetworks = mapper.Map<List<NetworkWithTokensModel>>(networks);

        return Result.Ok(mappedNetworks).ToHttpResult();
    }

    private async static Task<IResult> GetAllSourcesAsync(
        RouteService routeService,
        IMapper mapper,
        SolverDbContext dbContext,
        HttpContext httpContext,
        [FromQuery(Name = "destination_network")] string? destinationNetwork,
        [FromQuery(Name = "destination_token")] string? destinationAsset)
    {
        var reachablePointsResult = await routeService.GetReachablePointsAsync(
           fromSrcToDest: false,
           network: destinationNetwork,
           asset: destinationAsset);

        return await MapToNetworkWithTokensAsync(mapper, dbContext, reachablePointsResult);
    }

    private async static Task<IResult> GetAllDestinationsAsync(
        IMapper mapper,
        RouteService routeService,
        SolverDbContext dbContext,
        SolverDbContext blockchainDbContext,
        HttpContext httpContext,
        [FromQuery(Name = "source_network")] string? sourceNetwork,
        [FromQuery(Name = "source_token")] string? sourceAsset)
    {
        var reachablePointsResult = await routeService.GetReachablePointsAsync(
           fromSrcToDest: true,
           network: sourceNetwork,
           asset: sourceAsset);

        return await MapToNetworkWithTokensAsync(mapper, dbContext, reachablePointsResult);
    }

    private async static Task<IResult> GetQuoteAsync(
        RouteService routeService,
        IMapper mapper,
        SolverDbContext blockchainDbContext,
        HttpContext httpContext,
        [AsParameters] GetQuoteQueryParams queryParams)
    {
        var quoteRequest = new QuoteRequest
        {
            SourceNetwork = queryParams.SourceNetwork!,
            SourceToken = queryParams.SourceAsset!,
            DestinationNetwork = queryParams.DestinationNetwork!,
            DestinationToken = queryParams.DestinationAsset!,
            Amount = queryParams.Amount!.Value,
        };

        var rateResult = await routeService.GetValidatedQuoteAsync(quoteRequest);

        if (rateResult.IsFailed)
        {
            return rateResult.ToHttpResult();
        }

        var route = rateResult.Value.Route;
        var mappedQuote = mapper.Map<Models.QuoteModel>(rateResult.Value);

        mappedQuote.TotalFeeInUsd = (mappedQuote.TotalFee * route.Source.UsdPrice).Truncate(UsdPrecision);

        return Result.Ok(mappedQuote).ToHttpResult();
    }

    private static async Task<IResult> MapToNetworkWithTokensAsync(
        IMapper mapper,
        SolverDbContext dbContext,
        Result<IEnumerable<Token>> reachablePointsResult)
    {
        if (reachablePointsResult.IsFailed)
        {
            return reachablePointsResult.ToHttpResult();
        }

        var networkNames = reachablePointsResult.Value.Select(x => x.Network.Name).Distinct();

        var nativeTokens = await dbContext.Tokens
            .Where(x => networkNames.Contains(x.Network.Name) && x.IsNative)
            .ToDictionaryAsync(x => x.Network.Name);

        var mappedNetworks = new List<NetworkWithTokensModel>();
        var groupingsByNetwork = reachablePointsResult.Value.GroupBy(x => x.Network);

        foreach (var grouping in groupingsByNetwork)
        {
            var network = grouping.Key;
            network.Tokens = grouping.ToList();
            var mappedNetwork = mapper.Map<NetworkWithTokensModel>(network);

            if (mappedNetwork.NativeToken == null)
            {
                mappedNetwork.NativeToken = mapper.Map<Models.TokenModel>(nativeTokens[network.Name]);
            }

            mappedNetworks.Add(mappedNetwork);
        }

        return Result.Ok(mappedNetworks).ToHttpResult();
    }

}
