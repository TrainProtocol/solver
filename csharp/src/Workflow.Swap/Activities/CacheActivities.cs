using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Cache;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Activities;

namespace Train.Solver.Workflow.Swap.Activities;

public class CacheActivities(IBalanceCache balanceCache) : ICacheActivities
{
    [Activity]
    public async Task UpdateNetworkBalanceAsync(string address, NetworkBalanceDto networkBalance)
    {
        await balanceCache.SetAsync(address, networkBalance, TimeSpan.FromMinutes(6));
    }
}
