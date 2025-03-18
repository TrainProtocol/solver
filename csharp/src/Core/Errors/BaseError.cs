using FluentResults;

namespace Train.Solver.Core.Errors;

public abstract class BaseError : Error
{
    public BaseError()
    {            
    }

    public BaseError(string message)
    {
        Message = message;
    }

    public abstract int HttpStatusCode { get; }

    public abstract string ErrorCode { get; }
}
