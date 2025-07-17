using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Workflows;

[Workflow]
public interface ITransactionProcessor
{
    [WorkflowRun]
    Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context);
}