using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class NonceMissMatchException(string message)
    : ApplicationFailureException(message, errorType: nameof(NonceMissMatchException))
{
}
