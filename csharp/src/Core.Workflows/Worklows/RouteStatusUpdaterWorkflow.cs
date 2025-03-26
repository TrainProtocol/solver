using Temporalio.Workflows;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows.Worklows;

[Workflow]
public class RouteStatusUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var allRoutes = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetAllRoutesAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var managedAddressesByNetwork = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetManagedAddressesByNetworkAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var groupedByNetworkAndAsset = allRoutes
            .GroupBy(route => new
            {
                route.Destionation.NetworkId,
                route.Destionation.NetworkType,
                route.Destionation.Asset
            });

        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkId = group.Key.NetworkId;
            var networkType = group.Key.NetworkType;
            var asset = group.Key.Asset;

            if (!managedAddressesByNetwork.TryGetValue(networkId, out var managedAddress))
            {
                continue;
            }

            BalanceResponse balance;

            try
            {
                var firstRoute = group.First();
                var networkName = firstRoute.Destionation.NetworkName;

                balance = await ExecuteActivityAsync<BalanceResponse>(
                    $"{networkType}{nameof(IBlockchainActivities.GetBalanceAsync)}",
                        [   
                            networkName,
                            managedAddress!,
                            asset,
                        ],
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }
            catch (Exception ex)
            {
                continue;
            }

            await ExecuteActivityAsync(
                (RouteActivities x) => x.UpdateDestinationRouteStatusAsync(
                    group.Select(route => route.Id), balance.Amount),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}