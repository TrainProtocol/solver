namespace Train.Solver.Core.Models;

public class NextNonceRequest : BaseRequest
{
    public required string Address { get; set; } = null!;
}
