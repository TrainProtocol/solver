using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFTrustedWalletRepository(SolverDbContext dbContext) : ITrustedWalletRepository
{
    public async Task<TrustedWallet?> CreateAsync(NetworkType type, string address, string name)
    {
        var trustedWalletExists = await dbContext.TrustedWallets.AnyAsync(x => x.Address == address);

        if (trustedWalletExists)
        {
            return null;
        }

        var trustedWallet = new TrustedWallet
        {
            Address = address,
            Name = name,
            NetworkType = type,
        };

        dbContext.TrustedWallets.Add(trustedWallet);
        await dbContext.SaveChangesAsync();

        return trustedWallet;
    }

    public async Task DeleteAsync(NetworkType type, string address)
    {
        await dbContext.TrustedWallets
            .Where(x => x.NetworkType == type && x.Address == address)
            .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<TrustedWallet>> GetAllAsync(NetworkType[]? types)
    {
        var trustedWallets = await dbContext.TrustedWallets
            .Where(x => types == null || types.Contains(x.NetworkType)).ToListAsync();
        return trustedWallets;
    }

    public async Task<TrustedWallet?> GetAsync(NetworkType type, string address)
    {
        var trustedWallet = await dbContext.TrustedWallets.FirstOrDefaultAsync(x => x.NetworkType == type && x.Address == address);
        return trustedWallet;
    }

    public async Task<TrustedWallet?> UpdateAsync(NetworkType type, string address, string name)
    {
        var trustedWallet = await GetAsync(type, address);


        if (trustedWallet == null)
        {
            return null;
        }

        trustedWallet.Name = name;
        await dbContext.SaveChangesAsync();

        return trustedWallet;
    }


}
