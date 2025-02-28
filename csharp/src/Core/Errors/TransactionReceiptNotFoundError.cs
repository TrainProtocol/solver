namespace Train.Solver.Core.Errors;

public class TransactionReceiptNotFoundError(string errorMessage) : NotFoundError(errorMessage)
{
    public override string ErrorCode => "TRANSACTION_RECEIPT_NOT_FOUND";  
}
