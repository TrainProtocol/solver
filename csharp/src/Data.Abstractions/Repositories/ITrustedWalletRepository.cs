using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ITrustedWalletRepository
{
    public Task<TrustedWallet?> GetAsync(NetworkType type, string address);

    public Task<IEnumerable<TrustedWallet>> GetAllAsync(NetworkType[]? filterTypes);

    public Task<TrustedWallet?> CreateAsync(CreateTrustedWalletRequest request);

    public Task<TrustedWallet?> UpdateAsync(NetworkType type, string address, UpdateTrustedWalletRequest request);

    public Task DeleteAsync(NetworkType type, string address);
}
