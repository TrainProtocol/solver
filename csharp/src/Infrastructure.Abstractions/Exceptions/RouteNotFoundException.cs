namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class RouteNotFoundException(string message) : UserFacingException(message)
{
}
