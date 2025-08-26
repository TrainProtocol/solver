using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Activities;
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
        var allRoutes = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetAllRoutesAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var enrties = allRoutes
            .SelectMany(r => new[]
            {
               new { Network = r.Source.Network.Name, Address = r.SourceWallet },
               new { Network = r.Destination.Network.Name, Address = r.DestinationWallet }
            })
            .Distinct()
            .ToList();

        foreach (var entry in enrties)
        {
            var networkBalance = await ExecuteChildWorkflowAsync(
                (IBalanceWorkflow wf) => wf.RunAsync(entry.Network, entry.Address),
                new ChildWorkflowOptions
                {

                    Id = TemporalHelper.BuildBalanceWorkflowId(entry.Network, Guid.NewGuid())
                });

            await ExecuteActivityAsync(
                (ICacheActivities x) => x.UpdateNetworkBalanceAsync(entry.Address, networkBalance),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}
