namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class UserFacingException(string message) 
    : Exception(message)
{
}
