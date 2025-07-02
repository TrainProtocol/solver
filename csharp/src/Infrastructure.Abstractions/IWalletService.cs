using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions;

public class CreateWalletRequest
{
    public string Name { get; set; } = null!;

    public NetworkType Type { get; set; }
}

public class UpdateWalletRequest
{
    public string Name { get; set; } = null!;
}

public interface IWalletService
{
    public Task<string> GetAsync(NetworkType type);

    public Task<IEnumerable<string>> GetAllAsync(NetworkType type);

    public Task<string> CreateAsync(CreateWalletRequest request);
}
