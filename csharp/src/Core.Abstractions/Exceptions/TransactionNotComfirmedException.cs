namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TransactionNotComfirmedException(string message) : Exception(message)
{
}
