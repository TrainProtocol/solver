using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InvalidRequestBodyError : BaseError
{
    public InvalidRequestBodyError() : base("Invalid request body")
    {
    }

    public override int HttpStatusCode => StatusCodes.Status500InternalServerError;

    public override string ErrorCode => "INVALID_REQUEST_BODY";
}
