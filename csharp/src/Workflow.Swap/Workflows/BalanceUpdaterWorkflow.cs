using System.Threading.Tasks;
using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;
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
public class BalanceUpdaterWorkflow : IScheduledWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var routes = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetAllRoutesAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var entries = routes
            .SelectMany(r => new[]
            {
                new { Network = r.Source.Network.Name, Address = r.SourceWallet },
                new { Network = r.Destination.Network.Name, Address = r.DestinationWallet }
            })
            .Distinct()
            .ToList();

        foreach (var e in entries)
        {
            var network = await ExecuteActivityAsync(
                (INetworkActivities x) => x.GetNetworkAsync(e.Network),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

            var balances = new List<TokenBalanceDto>();

            foreach (var token in network.Tokens)
            {
                var balance = await ExecuteActivityAsync(
                    (IBlockchainActivities x) => x.GetBalanceAsync(new BalanceRequest
                    {
                        Address = e.Address,
                        Network = network,
                        Asset = token.Symbol
                    }),
                    TemporalHelper.DefaultActivityOptions(network.Type.ToString()));

                balances.Add(new TokenBalanceDto
                {
                    Token = token,
                    Amount = balance.Amount
                });
            }

            var dto = new NetworkBalanceDto
            {
                Network = network,
                Balances = balances
            };

            await ExecuteActivityAsync(
                (ICacheActivities x) => x.UpdateNetworkBalanceAsync(e.Address, dto),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}