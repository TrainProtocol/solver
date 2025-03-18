using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Temporalio.Client;
using Train.Solver.API.Models;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services;
using Train.Solver.Core.Workflows;
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

        group.MapPost("/swaps/{commitId}/addLockSig", AddLockSigAsync)
            .Produces<ApiResponse>();

        return group;
    }

    private async static Task<IResult> AddLockSigAsync(
    [FromServices] SolverDbContext dbContext,
    [FromServices] ITemporalClient temporalClient,
    [FromServices] IServiceProvider serviceProvider,
    [FromRoute] string commitId,
    [FromBody] AddLockSignatureModel addLockSignature)
    {
        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .FirstOrDefaultAsync(x => x.Id == commitId);

        if (swap is null)
        {
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "SWAP_NOT_FOUND",
                    Message = "Swap was not found.",
                }
            });
        }

        var addLockSignatureRequest = new AddLockSignatureRequest
        {
            Asset = swap.SourceToken.Asset,
            Hashlock = swap.Hashlock,
            Id = swap.Id,
            SignerAddress = swap.SourceAddress,
            Signature = addLockSignature.Signature,
            SignatureArray = addLockSignature.SignatureArray,
            Timelock = addLockSignature.Timelock,
            V = addLockSignature.V,
            R = addLockSignature.R,
            S = addLockSignature.S,
        };

        var isValid = await temporalClient
            .GetWorkflowHandle<SwapWorkflow>(commitId)
            .ExecuteUpdateAsync((x) => x.SetAddLockSigAsync(swap.SourceToken.Network.Name, addLockSignatureRequest));

        if (!isValid)
        {
            return Results.BadRequest(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "INVALID_SIGNATURE",
                    Message = "Invalid signature",
                }
            });
        }

        return Results.Ok(new ApiResponse());
    }

    private static async Task<IResult> GetSwapAsync(
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
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "SWAP_NOT_FOUND",
                    Message = "Swap not found",
                }
            });
        }

        var mappedSwap = mapper.Map<SwapModel>(swap);
        return Results.Ok(new ApiResponse<SwapModel> { Data = mappedSwap });
    }

    private static async Task<IResult> GetAllSwapsAsync(
        SolverDbContext dbContext,
        IMapper mapper,
        [FromQuery] string[]? addresses,
        [FromQuery] uint? page)
    {
        var pageSize = 20;
        if (addresses != null && addresses.Length > 6)
        {
            return Results.BadRequest(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "TOO_MANY_ADDRESSES",
                    Message = "Too many addresses provided",
                }
            });
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
            return Results.Ok(new ApiResponse<IEnumerable<SwapModel>> { Data = Enumerable.Empty<SwapModel>() });
        }

        return Results.Ok(new ApiResponse<IEnumerable<SwapModel>> { Data = mapper.Map<List<SwapModel>>(swaps) });
    }

    private static async Task<IResult> GetSwapRouteLimitsAsync(
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
                SourceToken = queryParams.SourceToken!,
                DestinationNetwork = queryParams.DestinationNetwork!,
                DestinationToken = queryParams.DestinationToken!,
            });

        if (limitResult == null)
        {
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "LIMIT_NOT_FOUND",
                    Message = "Limit not found",
                }
            });
        }

        var route = limitResult.Route;
        var mappedLimit = mapper.Map<Models.LimitModel>(limitResult);

        mappedLimit.MaxAmountInUsd = (mappedLimit.MaxAmount * route.Source.UsdPrice).Truncate(UsdPrecision);
        mappedLimit.MinAmountInUsd = (mappedLimit.MinAmount * route.Source.UsdPrice).Truncate(UsdPrecision);

        return Results.Ok(new ApiResponse<Models.LimitModel> { Data = mappedLimit });
    }

    private static async Task<IResult> GetNetworksAsync(
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

        return Results.Ok(new ApiResponse<List<NetworkWithTokensModel>> { Data = mappedNetworks });
    }

    private static async Task<IResult> GetAllSourcesAsync(
        RouteService routeService,
        IMapper mapper,
        SolverDbContext dbContext,
        HttpContext httpContext,
        [FromQuery] string? destinationNetwork,
        [FromQuery] string? destinationToken)
    {
        var reachablePointsResult = await routeService.GetReachablePointsAsync(
            fromSrcToDest: false,
            network: destinationNetwork,
            asset: destinationToken);

        if (reachablePointsResult == null)
        {
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "REACHABLE_POINTS_NOT_FOUND",
                    Message = "No reachable points found",
                }
            });
        }

        return await MapToNetworkWithTokensAsync(mapper, dbContext, reachablePointsResult);
    }

    private static async Task<IResult> GetAllDestinationsAsync(
        IMapper mapper,
        RouteService routeService,
        SolverDbContext dbContext,
        SolverDbContext blockchainDbContext,
        HttpContext httpContext,
        [FromQuery] string? sourceNetwork,
        [FromQuery] string? sourceToken)
    {
        var reachablePointsResult = await routeService.GetReachablePointsAsync(
            fromSrcToDest: true,
            network: sourceNetwork,
            asset: sourceToken);

        return await MapToNetworkWithTokensAsync(mapper, dbContext, reachablePointsResult);
    }

    private static async Task<IResult> GetQuoteAsync(
        RouteService routeService,
        IMapper mapper,
        SolverDbContext blockchainDbContext,
        HttpContext httpContext,
        [AsParameters] GetQuoteQueryParams queryParams)
    {
        var quoteRequest = new QuoteRequest
        {
            SourceNetwork = queryParams.SourceNetwork!,
            SourceToken = queryParams.SourceToken!,
            DestinationNetwork = queryParams.DestinationNetwork!,
            DestinationToken = queryParams.DestinationToken!,
            Amount = queryParams.Amount!.Value,
        };

        var rateResult = await routeService.GetValidatedQuoteAsync(quoteRequest);

        if (rateResult == null)
        {
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    Code = "QUOTE_NOT_FOUND",
                    Message = "Quote not found",
                }
            });
        }

        var route = rateResult.Route;
        var mappedQuote = mapper.Map<Models.QuoteModel>(rateResult);

        mappedQuote.TotalFeeInUsd = (mappedQuote.TotalFee * route.Source.UsdPrice).Truncate(UsdPrecision);

        return Results.Ok(new ApiResponse<Models.QuoteModel> { Data = mappedQuote });
    }

    private static async Task<IResult> MapToNetworkWithTokensAsync(
        IMapper mapper,
        SolverDbContext dbContext,
        IEnumerable<Token>? reachablePointsResult)
    {
        if (reachablePointsResult == null || !reachablePointsResult.Any())
        {
            return Results.Ok(new ApiResponse<List<NetworkWithTokensModel>>());
        }

        var networkNames = reachablePointsResult.Select(x => x.Network.Name).Distinct();

        var nativeTokens = await dbContext.Tokens
            .Where(x => networkNames.Contains(x.Network.Name) && x.IsNative)
            .ToDictionaryAsync(x => x.Network.Name);

        var mappedNetworks = new List<NetworkWithTokensModel>();
        var groupingsByNetwork = reachablePointsResult.GroupBy(x => x.Network);

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

        return Results.Ok(new ApiResponse<List<NetworkWithTokensModel>> { Data = mappedNetworks });
    }
}
