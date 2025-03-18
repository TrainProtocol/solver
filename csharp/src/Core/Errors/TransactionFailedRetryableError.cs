namespace Train.Solver.Core.Errors;

public class TransactionFailedRetryableError(string errorMessage) : NotFoundError(errorMessage)
{
    public override string ErrorCode => "TRANSACTION_RETRYABLE_FAILED";
}
