using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class HTLCAlreadyExistsError : BaseError
{
    public HTLCAlreadyExistsError(string message)
    {
        Message = message;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "HASHLOCK_ALREADY_EXISTS_ERROR";
}
