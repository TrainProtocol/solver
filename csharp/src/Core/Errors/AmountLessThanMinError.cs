namespace Train.Solver.Core.Errors;

public class AmountLessThanMinError(string message) : BadRequestError(message)
{
    public override string ErrorCode => "LESS_THAN_MIN_ERROR";
}