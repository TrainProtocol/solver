using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InvalidApiKeyError : BaseError
{
    public InvalidApiKeyError()
        : base("Api key forbidden")
    {
    }

    public override int HttpStatusCode => StatusCodes.Status403Forbidden;

    public override string ErrorCode => "API_KEY_FORBIDDEN";
}