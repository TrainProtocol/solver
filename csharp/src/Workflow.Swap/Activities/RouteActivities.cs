using System.Numerics;
using Temporalio.Activities;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Common.Helpers;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Workflow.Abstractions.Activities;

namespace Train.Solver.Workflow.Swap.Activities;

public class RouteActivities(
    IRouteRepository routeRepository, 
    KeyedServiceResolver<IRateProvider> rateProviderResolver) : IRouteActivities
{
    [Activity]
    public async Task<List<RouteDetailedDto>> GetAllRoutesAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active, RouteStatus.Inactive]);

        return routes
            .Select(r => r.ToDetailedDto())
            .ToList();
    }

    [Activity]
    public async Task<List<NetworkDto>> GetActiveSolverRouteSourceNetworksAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);

        return routes
            .Select(x => x.SourceToken.Network.ToDto())
            .DistinctBy(x => x.Name)
            .ToList();
    }

    [Activity]
    public virtual async Task UpdateRoutesStatusAsync(
        int[] routeIds,
        RouteStatus status)
    {
        await routeRepository.UpdateRoutesStatusAsync(routeIds, status);
    }

    [Activity]
    public virtual async Task<BigInteger> ConvertToSourceAsync(
        RouteDetailedDto route,
        BigInteger destinationAmount)
    {
        var rateProvider = rateProviderResolver.Resolve(route.RateProviderName);

        if (rateProvider is null)
        {
            throw new ArgumentException($"Rate provider {route.RateProviderName} not found.");
        }


        var rate = await rateProvider.GetRateAsync(new RouteDto
        {
            Source = route.Source,
            Destination = route.Destination,
        });

        var sourceAmount = destinationAmount.ConvertSendAmount(
            route.Destination.Token.Decimals,
            route.Source.Token.Decimals,
            rate);

        return sourceAmount;
    }
}
