namespace Train.Solver.Core.Abstractions.Models;

public class AllowanceRequest : BaseRequest
{
    public string OwnerAddress { get; set; } = null!;

    public string SpenderAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;
}
