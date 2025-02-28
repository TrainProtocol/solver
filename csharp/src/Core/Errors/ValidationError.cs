namespace Train.Solver.Core.Errors;

public class ValidationError(string message) : BadRequestError(message)
{
    public ValidationError() : this("Invalid request")
    {
    }

    public override string ErrorCode => "VALIDATION_ERROR";
}
