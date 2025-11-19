using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class HashlockAlreadySetException(string message) 
    : ApplicationFailureException(message, errorType: nameof(HashlockAlreadySetException))
{
}
