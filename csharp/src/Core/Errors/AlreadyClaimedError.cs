using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class AlreadyClaimedError : BaseError
{
    public AlreadyClaimedError(string message)
    {
        Message = message;
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;

    public override string ErrorCode => "ALREADY_CLAIMED_ERROR";
}
