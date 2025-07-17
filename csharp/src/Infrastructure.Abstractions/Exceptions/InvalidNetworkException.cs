namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class InvalidNetworkException(string message) : UserFacingException(message)
{
}