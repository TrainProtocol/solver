namespace Train.Solver.Core.Services;
public interface ITokenPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync();
}