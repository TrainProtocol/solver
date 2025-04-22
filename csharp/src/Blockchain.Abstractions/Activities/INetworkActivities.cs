using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface INetworkActivities
{
    [Activity]
    Task<DetailedNetworkDto> GetNetworkAsync(string networkName);

    [Activity]
    Task<List<TokenNetworkDto>> GetAvailableTokensAsync();
}
