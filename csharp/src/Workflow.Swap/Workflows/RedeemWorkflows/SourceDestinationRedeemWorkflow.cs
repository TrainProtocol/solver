using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Swap.Workflows.RedeemWorkflows;
 
public class SourceDestinationRedeemWorkflow : BaseWorkflow
{
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