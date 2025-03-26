namespace Train.Solver.Core.Abstractions.Exceptions;

public class TransactionFailedRetriableException(string message) : Exception(message)
{
}
