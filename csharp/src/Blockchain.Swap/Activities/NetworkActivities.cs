﻿using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.Blockchain.Swap.Activities;

public class NetworkActivities(INetworkRepository networkRepository) : INetworkActivities
{
    [Activity]
    public async Task<DetailedNetworkDto> GetNetworkAsync(string networkName)
    {
        var network = await networkRepository.GetAsync(networkName);

        if (network == null)
        {
            throw new ArgumentException($"Network {networkName} not found");
        }

        return network.ToDetailedDto();
    }
}
