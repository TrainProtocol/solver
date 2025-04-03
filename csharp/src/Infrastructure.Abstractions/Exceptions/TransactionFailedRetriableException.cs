namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionFailedRetriableException(string message) : Exception(message)
{
}
