namespace Train.Solver.Core.Errors;

public class InvalidTimelockError : BadRequestError
{
    public InvalidTimelockError(string message) : base(message)
    {
        Message = message;
    }

    public override string ErrorCode => "INVALID_TIMELOCK_ERROR";
}
