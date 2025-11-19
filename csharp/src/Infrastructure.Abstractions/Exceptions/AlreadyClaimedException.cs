using Temporalio.Exceptions;

namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class AlreadyClaimedException(string message) 
    : ApplicationFailureException(message, errorType: nameof(AlreadyClaimedException))
{
}
