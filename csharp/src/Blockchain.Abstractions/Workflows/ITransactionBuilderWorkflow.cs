using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

public interface ITransactionBuilderWorkflow
{
    [WorkflowRun]
    Task<PrepareTransactionResponse> RunAsync(TransactionBuilderRequest message);
}
