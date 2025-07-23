using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

public interface ITransactionBuilderWorkflow
{
    Task<PrepareTransactionResponse> RunAsync(PrepareTransactionRequest message);
}
