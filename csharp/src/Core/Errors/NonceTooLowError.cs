namespace Train.Solver.Core.Errors;

public class NonceTooLowError(string message) : BadRequestError(message)
{
    public override string ErrorCode => "NONCE_TOO_LOW";
}
