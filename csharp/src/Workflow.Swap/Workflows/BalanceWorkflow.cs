using System.Numerics;
using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using static Temporalio.Workflows.Workflow;
using static Train.Solver.Workflow.Common.Helpers.TemporalHelper;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class BalanceWorkflow : IBalanceWorkflow
{
    [WorkflowRun]
    public async Task<Dictionary<TokenDto, BigInteger>> RunAsync(string networkName, string address)
    {
        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(networkName),
               DefaultActivityOptions(Constants.CoreTaskQueue));

        var balances = new Dictionary<TokenDto, BigInteger>();

        foreach (var token in network.Tokens)
        {
            var balance = await ExecuteActivityAsync(
                (IBlockchainActivities x) => x.GetBalanceAsync(new BalanceRequest()
                {
                    Address = address,
                    Network = network,
                    Asset = token.Symbol,
                }),
                DefaultActivityOptions(network.Type.ToString()));

            balances[token] = balance.Amount;
        }

        return balances;
    }
}