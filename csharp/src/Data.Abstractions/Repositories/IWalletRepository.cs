using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public class CreateWalletRequest
{
    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public NetworkType Type { get; set; }
}

public interface IWalletRepository
{
    public Task<Wallet> GetDefaultAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAsync(NetworkType type);

    public Task<IEnumerable<Wallet>> GetAllAsync();

    public Task CreateAsync(CreateWalletRequest request);


}
