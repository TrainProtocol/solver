namespace Train.Solver.Workflow.Abstractions.Models;

public class SpenderAddressRequest : BaseRequest
{
    public required string Asset { get; set; }
}
