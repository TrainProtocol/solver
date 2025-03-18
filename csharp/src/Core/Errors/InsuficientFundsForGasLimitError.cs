using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InsuficientFundsForGasLimitError : BaseError
{
    public InsuficientFundsForGasLimitError(string errorMessage)
    {
        Message = errorMessage;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "INSUFFICIENT_FUNDS_TO_ESTIMATE_ERROR";
}
