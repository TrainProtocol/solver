namespace Train.Solver.Core.Errors;

public class InvalidAddressError(string errorMessage) : BadRequestError(errorMessage)
{
    public override string ErrorCode => "INVALID_ADDRESS";
}
