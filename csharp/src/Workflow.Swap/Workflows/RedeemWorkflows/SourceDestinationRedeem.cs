using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Swap.Workflows.RedeemWorkflows;

public class SourceDestinationRedeem : BaseWorkflow
{
    [WorkflowRun]
    public async Task RedeemAsync(
        TransactionRequest sourceRedeemRequest,
        TransactionRequest destinationRedeemRequest)
    {
        // Redeem user funds
        var redeemInDestinationTask = ExecuteTransactionAsync(destinationRedeemRequest);

        // Redeem LP funds
        var redeemInSourceTask = ExecuteTransactionAsync(sourceRedeemRequest);

        await Task.WhenAll(
            redeemInDestinationTask,
            redeemInSourceTask);
    }
}