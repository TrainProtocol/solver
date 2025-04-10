using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Swap.Activities;
using Train.Solver.Data.Abstractions.Entities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class RouteStatusUpdaterWorkflow : IScheduledWorkflow
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
                route.Destionation.Network.Name,
                route.Destionation.Network.Type,
                route.Destionation.Symbol
            });

        var networkNames = groupedByNetworkAndAsset.Select(x => x.Key.Name).Distinct().ToArray();

        var managedAddressesByNetwork = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetSolverAddressesAsync(networkNames),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));


        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkName = group.Key.Name;
            var networkType = group.Key.Type;
            var asset = group.Key.Symbol;

            if (!managedAddressesByNetwork.TryGetValue(networkName, out var managedAddress))
            {
                continue;
            }

            BalanceResponse balance;

            try
            {
                balance = await ExecuteActivityAsync((IBlockchainActivities x) => x.GetBalanceAsync(new BalanceRequest
                {
                    NetworkName = networkName,
                    Address = managedAddress!,
                    Asset = asset
                }), TemporalHelper.DefaultActivityOptions(networkType));
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
                    (IRouteActivities x) => x.UpdateRoutesStatusAsync(
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
                    (IRouteActivities x) => x.UpdateRoutesStatusAsync(
                        group.Select(route => route.Id).ToArray(),
                        RouteStatus.Active),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }
        }
    }
}