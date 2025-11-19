using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class HTLCAlreadyExistsException(string message)
    : ApplicationFailureException(message, errorType: nameof(HTLCAlreadyExistsException))
{
}
