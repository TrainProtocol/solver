namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionFailedException(string message) : Exception(message)
{
}
