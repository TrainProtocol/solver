namespace Train.Solver.Core.Services.TokenPrice;
public interface ITokenPriceService
{
    Task<Dictionary<string, decimal>> GetPricesAsync(params string[] tokenSymbols);

    Task UpdatePricesAsync(Dictionary<string, decimal> prices);
}