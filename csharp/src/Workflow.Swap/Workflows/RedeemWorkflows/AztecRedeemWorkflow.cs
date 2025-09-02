using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Swap.Workflows.RedeemWorkflows;

public class AztecRedeemWorkflow : BaseWorkflow
{
    [WorkflowRun]
    public async Task RedeemAsync(TransactionRequest redeemRequest)
    {
        // Redeem LP funds
        await ExecuteTransactionAsync(redeemRequest);
    }
}
