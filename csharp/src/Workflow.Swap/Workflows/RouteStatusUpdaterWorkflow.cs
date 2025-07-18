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

            var balanceInWei = BigInteger.Parse(balance.AmountInWei);

            var routesToDisable = group
                .Where(route => route.Status == RouteStatus.Active && BigInteger.Parse(route.MaxAmountInSource) > balanceInWei)
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
                .Where(route => route.Status == RouteStatus.Inactive && BigInteger.Parse(route.MaxAmountInSource) <= balanceInWei)
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
