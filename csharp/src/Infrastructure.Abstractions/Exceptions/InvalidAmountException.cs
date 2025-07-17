namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class InvalidAmountException(string message) : UserFacingException(message)
{
}
