namespace Train.Solver.Core.Abstractions.Models;

public class NextNonceRequest : BaseRequest
{
    public required string Address { get; set; } = null!;
}
