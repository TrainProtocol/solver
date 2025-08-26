using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface ICacheActivities
{
    [Activity]
    Task UpdateNetworkBalanceAsync(string address, NetworkBalanceDto networkBalance);
}