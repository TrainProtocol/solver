using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Npgsql;

public class EFWalletRepository(SolverDbContext dbContext) : IWalletRepository
{
    public async Task<Wallet?> CreateAsync(NetworkType type, string address, string name)
    {
        var walletExists = await dbContext.Wallets.AnyAsync(x => x.Address == address);

        if (walletExists)
        {
            return null;
        }

        var wallet = new Wallet
        {
            Address = address,
            Name = name,
            NetworkType = type,
        };

        dbContext.Wallets.Add(wallet);
        await dbContext.SaveChangesAsync();

        return wallet;
    }

    public async Task<IEnumerable<Wallet>> GetAllAsync(NetworkType[]? types)
    {
        var wallets = await dbContext.Wallets
            .Where(x => types == null || types.Contains(x.NetworkType)).ToListAsync();
        return wallets;
    }

    public async Task<Wallet?> GetAsync(NetworkType type, string address)
    {
        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(x => x.NetworkType == type && x.Address == address);
        return wallet;
    }

    public async Task<Wallet?> UpdateAsync(NetworkType type, string address, string name)
    {
        var wallet = await GetAsync(type, address);


        if (wallet == null)
        {
            return null;
        }

        wallet.Name = name;
        await dbContext.SaveChangesAsync();

        return wallet;
    }
}
