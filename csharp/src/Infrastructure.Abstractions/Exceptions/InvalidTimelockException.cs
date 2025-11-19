using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class InvalidTimelockException(string message) 
    : ApplicationFailureException(message, errorType: nameof(InvalidTimelockException))
{
}
