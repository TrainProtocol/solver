using Microsoft.AspNetCore.Mvc;
using Temporalio.Client;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Data.Npgsql;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.PublicAPI.Models;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;

namespace Train.Solver.PublicAPI.Endpoints;

public static class SolverV1Endpoints
{
    public static RouteGroupBuilder MapV1Endpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/routes", GetRoutesAsync)
           .Produces<ApiResponse<List<RouteDto>>>();

        group.MapGet("/quote", GetQuoteAsync)
            .Produces<ApiResponse<QuoteWithSolverDto>>();

        group.MapGet("/swaps/{commitId}", GetSwapAsync)
            .Produces<ApiResponse<SwapDto>>();

        group.MapPost("/transactions/build", BuildTransactionAsync)
            .Produces<ApiResponse<PrepareTransactionDto>>();

        group.MapPost("/swaps/{commitId}/addLockSig", AddLockSigAsync)
            .Produces<ApiResponse>();

        return group;
    }

    private async static Task<IResult> AddLockSigAsync(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
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
                    //Code = "SWAP_NOT_FOUND",
                    Message = "Swap not found",
                }
            });
        }

        var sourceNetwork = await networkRepository.GetAsync(swap.Route.SourceToken.Network.Name);

        if (sourceNetwork is null)
        {
            return Results.NotFound(new ApiResponse()
            {
                Error = new ApiError()
                {
                    //Code = "NETWORK_NOT_FOUND",
                    Message = "Source network not found",
                }
            });
        }

        var isValid = await temporalClient
            .GetWorkflowHandle<ISwapWorkflow>(commitId)
            .ExecuteUpdateAsync((x) => x.SetAddLockSigAsync(
                new AddLockSignatureRequest
                {
                    Asset = swap.Route.SourceToken.Asset,
                    Hashlock = swap.Hashlock,
                    CommitId = swap.CommitId,
                    SignerAddress = swap.SourceAddress,
                    Signature = addLockSignature.Signature,
                    SignatureArray = addLockSignature.SignatureArray,
                    Timelock = addLockSignature.Timelock,
                    V = addLockSignature.V,
                    R = addLockSignature.R,
                    S = addLockSignature.S,
                    Network = sourceNetwork.ToDetailedDto(),
                }));

        if (!isValid)
        {
            return Results.BadRequest(new ApiResponse()
            {
                Error = new ApiError()
                {
                    //Code = "INVALID_SIGNATURE",
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
                    //Code = "SWAP_NOT_FOUND",
                    Message = "Swap not found",
                }
            });
        }

        return Results.Ok(new ApiResponse<SwapDto> { Data = swap.ToDto() });
    }
    private static async Task<IResult> GetRoutesAsync(
        HttpContext httpContext,
        IRouteRepository routeRepository)
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);
        var mappedRoutes = routes.Select(x => x.ToDto());

        return Results.Ok(new ApiResponse<IEnumerable<RouteDto>> { Data = mappedRoutes });
    }

    private static async Task<IResult> GetQuoteAsync(
        IQuoteService routeService,
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

        return Results.Ok(new ApiResponse<QuoteWithSolverDto> { Data = quote });
    }

    private static async Task<IResult> BuildTransactionAsync(
        ITemporalClient temporalClient,
        [FromBody] PrepareTransactionRequest request)
    {
        var prepareTransactionResponse = await temporalClient
            .ExecuteWorkflowAsync<PrepareTransactionDto>(
                "TransactionBuilderWorkflow", 
                args: [request],
                new(id: Guid.CreateVersion7().ToString(), taskQueue: "Core"));

        return Results.Ok(new ApiResponse<PrepareTransactionDto> { Data = prepareTransactionResponse });
    }
}
