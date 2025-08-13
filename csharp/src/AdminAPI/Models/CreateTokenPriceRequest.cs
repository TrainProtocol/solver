namespace Train.Solver.AdminAPI.Models;

public class CreateTokenPriceRequest
{
    public string Symbol { get; set; } = null!;

    public string ExternalId { get; set; } = null!;
}
