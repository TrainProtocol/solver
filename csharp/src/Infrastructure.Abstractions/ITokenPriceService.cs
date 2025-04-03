namespace Train.Solver.Infrastructure.Abstractions;
public interface ITokenPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync();
}