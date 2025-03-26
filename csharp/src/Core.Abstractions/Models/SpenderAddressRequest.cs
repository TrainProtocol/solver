namespace Train.Solver.Core.Abstractions.Models;

public class SpenderAddressRequest : BaseRequest
{
    public required string Asset { get; set; }
}
