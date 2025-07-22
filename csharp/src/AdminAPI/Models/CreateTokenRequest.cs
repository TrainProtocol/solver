namespace Train.Solver.AdminAPI.Models;

public class CreateTokenRequest
{
    public string Symbol { get; set; } = default!;
    public string? Contract { get; set; }
    public int Decimals { get; set; }
}
