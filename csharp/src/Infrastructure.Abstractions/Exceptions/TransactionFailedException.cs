using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionFailedException(string message)
    : ApplicationFailureException(message, errorType: nameof(TransactionFailedException))
{
}
