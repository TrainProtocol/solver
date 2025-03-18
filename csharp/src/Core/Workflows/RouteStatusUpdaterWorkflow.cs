using Serilog;
using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows;

[Workflow]
public class RouteStatusUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        Log.Information($"{nameof(RouteStatusUpdaterWorkflow)} is started");

        var allRoutes = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetAllRoutesAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        var managedAddressesByNetwork = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetManagedAddressesByNetworkAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        var groupedByNetworkAndAsset = allRoutes
            .GroupBy(route => new
            {
                route.Destionation.NetworkId,
                route.Destionation.NetowrkGroup,
                route.Destionation.Asset
            });

        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkId = group.Key.NetworkId;
            var networkGroup = group.Key.NetowrkGroup;
            var asset = group.Key.Asset;

            if (!managedAddressesByNetwork.TryGetValue(networkId, out var managedAddress))
            {
                Log.Error(
                    "No managed address found for NetworkId: {NetworkId}. Skipping group (Asset: {Asset}).",
                    networkId,
                    asset);
                continue;
            }

            BalanceModel balance;

            try
            {
                var firstRoute = group.First();
                var networkName = firstRoute.Destionation.NetworkName;

                balance = await ExecuteActivityAsync<BalanceModel>(
                    $"{networkGroup}{nameof(IBlockchainActivities.GetBalanceAsync)}",
                        [   
                            networkName,
                            managedAddress!,
                            asset,
                        ],
                    TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to get balance for networkId={NetworkId}, asset={Asset}, address={Address}. Skipping.",
                    networkId, asset, managedAddress);
                continue;
            }

            await ExecuteActivityAsync(
                (RouteActivities x) => x.UpdateDestinationRouteStatusAsync(
                    group.Select(route => route.Id), balance.Amount),
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }
    }
}