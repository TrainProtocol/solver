using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class BadRequestError : BaseError
{
    public BadRequestError(string errorMessage)
    {
        Message = errorMessage;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "BAD_REQUEST";
}