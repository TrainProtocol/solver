namespace Train.Solver.Core.Abstractions.Exceptions;

public class TransactionFailedException(string message) : Exception(message)
{
}
