namespace Train.Solver.Core.Errors;

public class TransactionUnderpricedError(string message) : BadRequestError(message)
{
    public TransactionUnderpricedError() : this("Transaction underpriced")
    {
    }

    public override string ErrorCode => "TRANSACTION_UNDERPRICED";
}
