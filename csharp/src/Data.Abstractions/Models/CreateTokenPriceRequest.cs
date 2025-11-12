namespace Train.Solver.Data.Abstractions.Models;

public class CreateTokenPriceRequest
{
    public string Symbol { get; set; } = null!;

    public string ExternalId { get; set; } = null!;
}
