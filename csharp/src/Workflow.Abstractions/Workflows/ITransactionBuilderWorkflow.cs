using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Workflows;

public interface ITransactionBuilderWorkflow
{
    Task<PrepareTransactionDto> RunAsync(PrepareTransactionRequest message);
}
