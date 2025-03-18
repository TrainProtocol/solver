using Train.Solver.Core.Temporal.Abstractions.Models;

namespace Train.Solver.Core.Temporal.Abstractions;

public interface ITransactionProcessor
{
    Task<TransactionModel> RunAsync(TransactionContext context);
}
