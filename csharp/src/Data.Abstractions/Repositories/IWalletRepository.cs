using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IWalletRepository
{
    public Task<Wallet?> GetDefaultAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAllAsync();

    public Task<Wallet?> CreateAsync(NetworkType type, string address, string name);

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, string name);
}
