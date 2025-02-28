namespace Train.Solver.Core.Errors;

public class TransactionFailedError(string errorMessage) : NotFoundError(errorMessage)
{
    public override string ErrorCode => "TRANSACTION_FAILED";  
}
