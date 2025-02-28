using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class UnhandledError : BaseError
{
    public UnhandledError() : base("Unhandled error")
    {
    }

    public override int HttpStatusCode => StatusCodes.Status500InternalServerError;

    public override string ErrorCode => "UNEXPECTED_ERROR";
}
