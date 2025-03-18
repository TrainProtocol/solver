using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class NotFoundError : BaseError
{
    public NotFoundError(string errorMessage)
    {
        Message = errorMessage;
    }

    public override int HttpStatusCode => StatusCodes.Status404NotFound;

    public override string ErrorCode => "NOT_FOUND";
}
