using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InsufficientFeeError : BaseError
{
    public InsufficientFeeError(string message)
    {
        Message = message;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "INSUFFICIENT_FEE_ERROR";
}
