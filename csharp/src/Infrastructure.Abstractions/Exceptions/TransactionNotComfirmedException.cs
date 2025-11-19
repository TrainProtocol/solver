using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionNotComfirmedException(string message)
    : ApplicationFailureException(message, errorType: nameof(TransactionNotComfirmedException))
{
}
