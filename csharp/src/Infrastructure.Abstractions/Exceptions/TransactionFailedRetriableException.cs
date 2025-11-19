using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionFailedRetriableException(string message) 
    : ApplicationFailureException(message, errorType: nameof(TransactionFailedRetriableException))

{
}
