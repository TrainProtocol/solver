﻿using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface IRouteActivities
{
    [Activity]
    Task<List<NetworkDto>> GetActiveSolverRouteSourceNetworksAsync();

    [Activity]
    Task<List<RouteDto>> GetAllRoutesAsync();

    [Activity]
    Task UpdateRoutesStatusAsync(int[] routeIds, RouteStatus status);
}