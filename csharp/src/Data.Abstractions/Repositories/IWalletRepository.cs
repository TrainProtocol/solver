using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Util.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IWalletRepository
{
    public Task<Wallet?> GetDefaultAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAllAsync();

    public Task<Wallet?> CreateAsync(NetworkType type, string address, string name);

    public Task<Wallet?> UpdateAsync(NetworkType type, string address, string name);
}
