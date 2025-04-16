using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

[Workflow]
public interface ITransactionProcessor
{
    [WorkflowRun]
    Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context);
}