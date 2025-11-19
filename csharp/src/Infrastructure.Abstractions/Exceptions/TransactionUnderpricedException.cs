using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionUnderpricedException(string message)
    : ApplicationFailureException(message, errorType: nameof(TransactionUnderpricedException))
{
}
