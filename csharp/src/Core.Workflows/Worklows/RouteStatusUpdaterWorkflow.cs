using Temporalio.Workflows;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
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

        var groupedByNetworkAndAsset = allRoutes
            .GroupBy(route => new
            {
                route.Destionation.NetworkName,
                route.Destionation.NetworkType,
                route.Destionation.Asset
            });

        var networkNames = groupedByNetworkAndAsset.Select(x => x.Key.NetworkName).Distinct().ToArray();

        var managedAddressesByNetwork = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetSolverAddressesAsync(networkNames),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));


        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkName = group.Key.NetworkName;
            var networkType = group.Key.NetworkType;
            var asset = group.Key.Asset;

            if (!managedAddressesByNetwork.TryGetValue(networkName, out var managedAddress))
            {
                continue;
            }

            BalanceResponse balance;

            try
            {
                balance = await ExecuteActivityAsync<BalanceResponse>(
                    "GetBalance",
                        [
                            new BalanceRequest
                            {
                                NetworkName = networkName,
                                Address = managedAddress!,
                                Asset = asset
                            }
                        ],
                    TemporalHelper.DefaultActivityOptions(networkType));
            }
            catch (Exception ex)
            {
                continue;
            }

            var routesToDisable = group
                .Where(route => route.Status == RouteStatus.Active && route.MaxAmountInSource > balance.Amount)
                .ToList();

            if (routesToDisable.Any())
            {
                await ExecuteActivityAsync(
                    (RouteActivities x) => x.UpdateRoutesStatusAsync(
                        group.Select(route => route.Id).ToArray(),
                        RouteStatus.Inactive),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }

            var routesToEnable = group
                .Where(route => route.Status == RouteStatus.Inactive && route.MaxAmountInSource <= balance.Amount)
                .ToList();

            if (routesToEnable.Any())
            {
                await ExecuteActivityAsync(
                    (RouteActivities x) => x.UpdateRoutesStatusAsync(
                        group.Select(route => route.Id).ToArray(),
                        RouteStatus.Active),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }
        }
    }
}