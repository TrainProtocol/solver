using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITokenPriceRepository
{
    Task<List<TokenPrice>> GetAllAsync();

    Task UpdateAsync(Dictionary<string, decimal> prices);
}
