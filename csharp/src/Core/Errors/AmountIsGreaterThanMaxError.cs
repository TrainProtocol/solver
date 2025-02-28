namespace Train.Solver.Core.Errors;

public class AmountIsGreaterThanMaxError(string message) : BadRequestError(message)
{
    public override string ErrorCode => "GREATER_THAN_MAX_ERROR";
}
