using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions;

namespace Train.Solver.Infrastructure.Services;

public class WalletService : IWalletService
{
    public Task<string> CreateAsync(CreateWalletRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetAllAsync(NetworkType type)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetAsync(NetworkType type)
    {
        throw new NotImplementedException();
    }
}
