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

    public async Task<IEnumerable<Wallet>> GetAllAsync()
    {
        var wallets = await dbContext.Wallets.ToListAsync();
        return wallets;
    }

    public async Task<IEnumerable<Wallet>> GetAsync(NetworkType type)
    {
        var wallets = await dbContext.Wallets.Where(x => x.NetworkType == type).ToListAsync();
        return wallets;
    }

    public async Task<Wallet?> GetDefaultAsync(NetworkType type)
    {
        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(x => x.NetworkType == type);
        return wallet;
    }

    public async Task<Wallet?> UpdateAsync(NetworkType type, string address, string name)
    {
        var wallet = await dbContext.Wallets.SingleOrDefaultAsync(x => x.Address == address && x.NetworkType == type);

        if (wallet == null)
        {
            return null;
        }

        wallet.Name = name;
        await dbContext.SaveChangesAsync();

        return wallet;
    }
}
