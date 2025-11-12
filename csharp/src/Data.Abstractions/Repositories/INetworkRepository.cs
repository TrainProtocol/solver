using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<Token?> GetTokenAsync(string networkName, string symbol);

    Task<IEnumerable<Network>> GetAllAsync(NetworkType[]? types);

    Task<Network?> CreateAsync(CreateNetworkRequest request);

    Task<Node?> CreateNodeAsync(string networkName, CreateNodeRequest request);

    Task<Token?> CreateTokenAsync(string networkName, CreateTokenRequest request);

    Task DeleteTokenAsync(string networkName, string symbol);

    Task DeleteNodeAsync(string networkName, string providerName);

    Task<Network?> UpdateAsync(string networkName, UpdateNetworkRequest request);
}