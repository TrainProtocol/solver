namespace Train.Solver.Core.Abstractions;
public interface ITokenPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync();
}