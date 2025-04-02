namespace Train.Solver.Blockchain.Abstractions.Models;

public class SpenderAddressRequest : BaseRequest
{
    public required string Asset { get; set; }
}
