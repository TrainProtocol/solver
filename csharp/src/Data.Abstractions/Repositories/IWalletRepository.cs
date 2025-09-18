using Microsoft.AspNetCore.Mvc;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IWalletRepository
{
    public Task<Wallet?> GetAsync(NetworkType type, string address);

    public Task<IEnumerable<Wallet>> GetAllAsync(NetworkType[]? filterTypes);

    public Task<Wallet?> CreateAsync(string address, CreateWalletRequest request);

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, UpdateWalletRequest request);
}
