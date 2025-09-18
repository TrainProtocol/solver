using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITokenPriceRepository
{
    Task<List<TokenPrice>> GetAllAsync();

    Task<TokenPrice?> GetAsync(string symbol);

    Task<TokenPrice?> CreateAsync(CreateTokenPriceRequest request);

    Task UpdateAsync(Dictionary<string, decimal> prices);
}
