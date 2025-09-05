using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Swap.Workflows.RedeemWorkflows;

public class AztecRedeemWorkflow : BaseWorkflow
{
    public async Task RedeemAsync(TransactionRequest destinationRedeemRequest)
    {
        // Redeem LP funds
        await ExecuteTransactionAsync(destinationRedeemRequest);
    }
}
