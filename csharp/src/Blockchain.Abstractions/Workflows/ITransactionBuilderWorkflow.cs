using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

public interface ITransactionBuilderWorkflow
{
    Task<PrepareTransactionResponse> RunAsync(TransactionBuilderRequest message);
}
