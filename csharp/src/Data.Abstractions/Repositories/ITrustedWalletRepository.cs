using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITrustedWalletRepository
{
    public Task<Wallet?> GetAsync(NetworkType type, string address);

    public Task<IEnumerable<Wallet>> GetAllAsync();

    public Task<Wallet?> CreateAsync(NetworkType type, string address, string name);

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, string name);

    public Task<Wallet?> DeleteAsync(NetworkType type, string address);
}
