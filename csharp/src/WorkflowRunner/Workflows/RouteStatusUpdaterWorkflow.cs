using Serilog;
using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class RouteStatusUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        Log.Information($"{nameof(RouteStatusUpdaterWorkflow)} is started");

        var allRoutes = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetAllRoutesAsync(),
            Constants.DefaultActivityOptions);

        var managedAddressesByNetwork = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetManagedAddressesByNetworkAsync(),
            Constants.DefaultActivityOptions);

        var groupedByNetworkAndAsset = allRoutes
            .GroupBy(route => new
            {
                route.DestionationTokenModel.NetworkId,
                route.DestionationTokenModel.Asset
            });

        foreach (var group in groupedByNetworkAndAsset)
        {
            var networkId = group.Key.NetworkId;
            var asset = group.Key.Asset;

            if (!managedAddressesByNetwork.TryGetValue(networkId, out var managedAddress))
            {
                Log.Error(
                    "No managed address found for NetworkId: {NetworkId}. Skipping group (Asset: {Asset}).",
                    networkId,
                    asset);
                continue;
            }

            decimal balance;

            try
            {
                var firstRoute = group.First();
                var networkName = firstRoute.DestionationTokenModel.NetworkName;

                balance = await ExecuteActivityAsync(
                    (BlockchainActivities x) => x.GetBalanceAsync(
                        networkName,
                        asset,
                        managedAddress!),
                    Constants.DefaultActivityOptions);
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
                    group.Select(route => route.Id), balance),
                Constants.DefaultActivityOptions);
        }
    }
}