using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITrustedWalletRepository
{
    public Task<TrustedWallet?> GetAsync(NetworkType type, string address);

    public Task<IEnumerable<TrustedWallet>> GetAllAsync();

    public Task<TrustedWallet?> CreateAsync(NetworkType type, string address, string name);

    public Task<TrustedWallet?> UpdateAsync(NetworkType type, string address, string name);

    public Task DeleteAsync(NetworkType type, string address);
}
