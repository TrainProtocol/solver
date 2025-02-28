using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InternalError : BaseError
{
    public InternalError(string errorMessage)
    {
        Message = errorMessage;
    }

    public override int HttpStatusCode => StatusCodes.Status500InternalServerError;

    public override string ErrorCode => "INTERNAL_ERROR";
}
