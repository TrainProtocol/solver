using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITokenPriceRepository
{
    Task<List<TokenPrice>> GetAllAsync();

    Task<TokenPrice?> GetAsync(string symbol);

    Task<TokenPrice?> CreateAsync(string symbol, string externalId);

    Task UpdateAsync(Dictionary<string, decimal> prices);
}
