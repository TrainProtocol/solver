using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class HashlockAlreadySetError : BaseError
{
    public HashlockAlreadySetError(string message)
    {
        Message = message;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "HASHLOCK_ALREADY_SET_ERROR";
}
