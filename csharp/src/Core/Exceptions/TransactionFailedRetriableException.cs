namespace Train.Solver.Core.Exceptions;

public class TransactionFailedRetriableException(string message) : Exception(message)
{
}
