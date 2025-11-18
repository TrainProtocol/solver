using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Npgsql;

public class EFWalletRepository(
    ISignerAgentRepository signerAgentRepository,
    SolverDbContext dbContext) : IWalletRepository
{
    public async Task<Wallet?> CreateAsync(string address, CreateWalletRequest request)
    {        
        var walletExists = await dbContext.Wallets.AnyAsync(x => x.Address == address);

        if (walletExists)
        {
            return null;
        }

        var signerAgent = await signerAgentRepository.GetAsync(request.SignerAgent);

        if (signerAgent == null)
        {
            throw new Exception("Signer agent not found");
        }

        if (!signerAgent.SupportedTypes.Contains(request.NetworkType))
        {
            throw new Exception("Unsupported type");
        }

        var wallet = new Wallet
        {
            Address = address,
            Name = request.Name,
            NetworkType = request.NetworkType,
            SignerAgentId = signerAgent.Id,
        };

        dbContext.Wallets.Add(wallet);
        await dbContext.SaveChangesAsync();

        return wallet;
    }

    public async Task<IEnumerable<Wallet>> GetAllAsync(NetworkType[]? types)
    {
        var wallets = await dbContext.Wallets
            .Include(x=>x.SignerAgent)
            .Where(x => types == null || types.Contains(x.NetworkType)).ToListAsync();
        return wallets;
    }

    public async Task<Wallet?> GetAsync(NetworkType type, string address)
    {
        var wallet = await dbContext.Wallets
            .Include(x => x.SignerAgent)
            .FirstOrDefaultAsync(x => x.NetworkType == type && x.Address == address);

        return wallet;
    }

    public async Task<Wallet?> UpdateAsync(NetworkType type, string address, UpdateWalletRequest request)
    {
        var wallet = await GetAsync(type, address);


        if (wallet == null)
        {
            return null;
        }

        wallet.Name = request.Name;
        await dbContext.SaveChangesAsync();

        return wallet;
    }
}
