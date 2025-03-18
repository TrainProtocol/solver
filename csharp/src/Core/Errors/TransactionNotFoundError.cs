namespace Train.Solver.Core.Errors;

public class TransactionNotFoundError(string errorMessage) : NotFoundError(errorMessage)
{
    public override string ErrorCode => "TRANSACTION_NOT_FOUND";  
}
