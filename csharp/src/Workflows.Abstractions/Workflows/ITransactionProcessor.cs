using Temporalio.Workflows;
using Train.Solver.Workflows.Abstractions.Models;

namespace Train.Solver.Workflows.Abstractions.Workflows;

[Workflow]
public interface ITransactionProcessor
{
    [WorkflowRun]
    Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context);
}