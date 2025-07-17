namespace Train.Solver.Workflows.Abstractions.Models;

public class SpenderAddressRequest : BaseRequest
{
    public required string Asset { get; set; }
}
