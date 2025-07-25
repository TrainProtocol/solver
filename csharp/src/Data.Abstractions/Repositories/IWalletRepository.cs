using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IWalletRepository
{
    public Task<Wallet?> GetAsync(NetworkType type, string address);

    public Task<IEnumerable<Wallet>> GetAllAsync(NetworkType[]? filterTypes);

    public Task<Wallet?> CreateAsync(NetworkType type, string address, string name);

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, string name);
}
