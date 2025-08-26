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
    public async Task<NetworkBalanceDto> RunAsync(string networkName, string address)
    {
        var network = await ExecuteActivityAsync(
               (INetworkActivities x) => x.GetNetworkAsync(networkName),
               DefaultActivityOptions(Constants.CoreTaskQueue));

        var balances = new List<TokenBalanceDto>();

        foreach (var token in network.Tokens)
        {
            var balanceResponse = await ExecuteActivityAsync(
                (IBlockchainActivities x) => x.GetBalanceAsync(new BalanceRequest()
                {
                    Address = address,
                    Network = network,
                    Asset = token.Symbol,
                }),
                DefaultActivityOptions(network.Type.ToString()));

            balances.Add(new TokenBalanceDto
            {
                Amount = balanceResponse.Amount,
                Token = token,
            });
        }

        var networkBalance = new NetworkBalanceDto()
        {
            Balances = balances,
            Network = network,
        };

        return networkBalance;
    }
}