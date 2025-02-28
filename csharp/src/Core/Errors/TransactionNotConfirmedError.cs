namespace Train.Solver.Core.Errors;

public class TransactionNotConfirmedError(string errorMessage) : NotFoundError(errorMessage)
{
    public override string ErrorCode => "TRANSACTION_NOT_CONFIRMED";
}
