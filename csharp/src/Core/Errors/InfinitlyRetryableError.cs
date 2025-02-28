using Microsoft.AspNetCore.Http;

namespace Train.Solver.Core.Errors;

public class InfinitlyRetryableError(int seconds) : BaseError
{
    public TimeSpan Interval { get; private set; } = TimeSpan.FromSeconds(seconds);

    public override int HttpStatusCode => StatusCodes.Status500InternalServerError;

    public override string ErrorCode => "INFINITE_RETRY_ERROR";

    public InfinitlyRetryableError() : this(3 * 60)
    {
    }
}
