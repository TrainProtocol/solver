using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFTrustedWalletRepository : ITrustedWalletRepository
{
    public Task<Wallet?> CreateAsync(NetworkType type, string address, string name)
    {
        throw new NotImplementedException();
    }

    public Task<Wallet?> DeleteAsync(NetworkType type, string address)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Wallet>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Wallet?> GetAsync(NetworkType type, string address)
    {
        throw new NotImplementedException();
    }

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, string name)
    {
        throw new NotImplementedException();
    }
}
