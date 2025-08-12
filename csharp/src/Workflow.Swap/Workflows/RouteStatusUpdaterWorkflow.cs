using System.Numerics;
using Temporalio.Workflows;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Swap.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
[TemporalJobSchedule(Chron = "*/5 * * * *")]
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
                route.Destination.Network.Name,
                route.DestinationWallet,
                route.Destination.Token.Symbol
            });

        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkName = group.Key.Name;
            var wallet = group.Key.DestinationWallet;
            var asset = group.Key.Symbol;

            var network = await ExecuteActivityAsync(
                (INetworkActivities x) => x.GetNetworkAsync(networkName),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

            BalanceResponse balance;

            try
            {
                balance = await ExecuteActivityAsync((IBlockchainActivities x) => x.GetBalanceAsync(new BalanceRequest
                {
                    Network = network,
                    Address = wallet!,
                    Asset = asset
                }), TemporalHelper.DefaultActivityOptions(network.Type));
            }
            catch (Exception ex)
            {
                continue;
            }


            var routesIdsToDisable = new List<int>();
            var routesIdsToEnable = new List<int>();

            foreach (var route in group)
            {
                var balanceInSource = await ExecuteActivityAsync(
                    (IRouteActivities x) => x.ConvertToSourceAsync(
                       route,
                       balance.Amount),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

                if (route.Status == RouteStatus.Active && balanceInSource < BigInteger.Parse(route.MaxAmountInSource))
                {
                    routesIdsToDisable.Add(route.Id);
                }
                else if (route.Status == RouteStatus.Inactive && balanceInSource >= BigInteger.Parse(route.MaxAmountInSource))
                {
                    routesIdsToEnable.Add(route.Id);
                }
            }

            if (routesIdsToDisable.Any())
            {
                await ExecuteActivityAsync(
                    (IRouteActivities x) => x.UpdateRoutesStatusAsync(
                        routesIdsToDisable.ToArray(),
                        RouteStatus.Inactive),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }

            if (routesIdsToEnable.Any())
            {
                await ExecuteActivityAsync(
                    (IRouteActivities x) => x.UpdateRoutesStatusAsync(
                        routesIdsToEnable.ToArray(),
                        RouteStatus.Active),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }
        }
    }
}
