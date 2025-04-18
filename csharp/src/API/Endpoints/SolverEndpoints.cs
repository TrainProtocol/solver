using Microsoft.AspNetCore.Mvc;
using Temporalio.Client;
using Train.Solver.API.Models;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.API.Endpoints;

public static class SolverEndpoints
{
    public const int UsdPrecision = 6;

    public static RouteGroupBuilder MapEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/networks", GetNetworksAsync)
            .Produces<ApiResponse<List<DetailedNetworkDto>>>()
            .CacheOutput();

        group.MapGet("/routes", GetRoutesAsync)
           .Produces<ApiResponse<List<RouteDto>>>()
           .CacheOutput();

        group.MapGet("/sources", GetAllSourcesAsync)
            .Produces<ApiResponse<List<DetailedNetworkDto>>>()
            .CacheOutput();

        group.MapGet("/destinations", GetAllDestinationsAsync)
            .Produces<ApiResponse<List<DetailedNetworkDto>>>()
            .CacheOutput();

        group.MapGet("/limits", GetSwapRouteLimitsAsync)
          .Produces<ApiResponse<LimitDto>>();

        group.MapGet("/quote", GetQuoteAsync)
            .Produces<ApiResponse<QuoteDto>>();

        group.MapGet("/swaps", GetAllSwapsAsync)
            .Produces<ApiResponse<SwapDto>>();

        group.MapGet("/swaps/{commitId}", GetSwapAsync)
            .Produces<ApiResponse<SwapDto>>();

        group.MapPost("/swaps/{commitId}/addLockSig", AddLockSigAsync)
            .Produces<ApiResponse>();

        group.MapGet("/health", () => Results.Ok())
            .Produces(StatusCodes.Status200OK);

        return group;
    }

    private async static Task<IResult> AddLockSigAsync(
    ISwapRepository swapRepository,
    ITemporalClient temporalClient,
    [FromRoute] string commitId,
    [FromBody] AddLockSignatureModel addLockSignature)
    {
        var swap = await swapRepository.GetAsync(commitId);

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

        var isValid = await temporalClient
            .GetWorkflowHandle<ISwapWorkflow>(commitId)
            .ExecuteUpdateAsync((x) => x.SetAddLockSigAsync(
                new AddLockSignatureRequest
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
                    NetworkName = swap.SourceToken.Network.Name,
                }));

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
        ISwapRepository swapRepository,
        [FromRoute] string commitId)
    {
        var swap = await swapRepository.GetAsync(commitId);

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

        return Results.Ok(new ApiResponse<SwapDto> { Data = swap.ToDto() });
    }

    private static async Task<IResult> GetAllSwapsAsync(
        ISwapRepository swapRepository,
        [FromQuery] string[]? addresses,
        [FromQuery] uint? page)
    {
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

        var swaps = await swapRepository.GetAllAsync(page: page ?? 1, addresses: addresses);

        if (!swaps.Any())
        {
            return Results.Ok(new ApiResponse<IEnumerable<SwapDto>> { Data = [] });
        }

        var mappedSwaps = swaps.Select(x => x.ToDto());

        return Results.Ok(new ApiResponse<IEnumerable<SwapDto>> { Data = mappedSwaps });
    }

    private static async Task<IResult> GetSwapRouteLimitsAsync(
        HttpContext httpContext,
        IRouteService routeService,
        [AsParameters] GetRouteLimitsQueryParams queryParams)
    {
        var limit = await routeService.GetLimitAsync(
            new()
            {
                SourceNetwork = queryParams.SourceNetwork!,
                SourceToken = queryParams.SourceToken!,
                DestinationNetwork = queryParams.DestinationNetwork!,
                DestinationToken = queryParams.DestinationToken!,
            });

        if (limit == null)
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

        return Results.Ok(new ApiResponse<LimitDto> { Data = limit });
    }

    private static async Task<IResult> GetNetworksAsync(
        HttpContext httpContext,
        INetworkRepository networkRepository)
    {
        var networks = await networkRepository.GetAllAsync();

        networks.ToList().ForEach(x =>
        {
            x.Nodes = x.Nodes.Where(x => x.Type == NodeType.Public).ToList();
        });

        var mappedNetworks = networks.Select(x=>x.ToDetailedDto());

       

        return Results.Ok(new ApiResponse<IEnumerable<DetailedNetworkDto>> { Data = mappedNetworks });
    }

    private static async Task<IResult> GetRoutesAsync(
        HttpContext httpContext,
        IRouteRepository routeRepository)
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);
        var mappedRoutes = routes.Select(x => x.ToDto());

        return Results.Ok(new ApiResponse<IEnumerable<RouteDto>> { Data = mappedRoutes });
    }

    private static async Task<IResult> GetAllSourcesAsync(
        IRouteService routeService,
        INetworkRepository networkRepository,
        [FromQuery] string? destinationNetwork,
        [FromQuery] string? destinationToken)
    {
        var sources = await routeService.GetSourcesAsync(
            networkName: destinationNetwork,
            token: destinationToken);

        if (sources == null || !sources.Any())
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

        return Results.Ok(new ApiResponse<IEnumerable<DetailedNetworkDto>> { Data = sources });
    }

    private static async Task<IResult> GetAllDestinationsAsync(
        IRouteService routeService,
        INetworkRepository networkRepository,
        [FromQuery] string? sourceNetwork,
        [FromQuery] string? sourceToken)
    {
        var destinations = await routeService.GetDestinationsAsync(
            networkName: sourceNetwork,
            token: sourceToken);

        if (destinations == null || !destinations.Any())
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

        return Results.Ok(new ApiResponse<IEnumerable<DetailedNetworkDto>> { Data = destinations });
    }

    private static async Task<IResult> GetQuoteAsync(
        IRouteService routeService,
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

        var quote = await routeService.GetValidatedQuoteAsync(quoteRequest);

        if (quote == null)
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

        return Results.Ok(new ApiResponse<QuoteDto> { Data = quote });
    }
}
