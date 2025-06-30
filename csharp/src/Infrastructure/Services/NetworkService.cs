using System.Xml.Linq;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.Infrastructure.Services;

public class NetworkService(INetworkRepository networkRepository) : INetworkService
{
    public async Task CreateAsync(DetailedNetworkDto network)
    {
        var existingNetwork = await networkRepository.GetAsync(network.Name);

        if (existingNetwork != null)
        {
            throw new InvalidNetworkException($"Network {network.Name} already exists");
        }

        var newNetwork = new Network
        {
            Name = network.Name,
            DisplayName = network.DisplayName,
            HTLCNativeContractAddress = network.HTLCNativeContractAddress,
            HTLCTokenContractAddress = network.HTLCTokenContractAddress,
            Type = network.Type,
            FeeType = network.FeeType,
            ChainId = network.ChainId,
            FeePercentageIncrease = network.FeePercentageIncrease,
        };
                

    }

    public async Task<IEnumerable<DetailedNetworkDto>> GetAllAsync()
    {
        var networks = await networkRepository.GetAllAsync();
        return networks.Select(x=> x.ToDetailedDto());
    }

    public async Task<DetailedNetworkDto> GetAsync(string name)
    {
        var network = await networkRepository.GetAsync(name);

        if (network == null)
        {
            throw new InvalidNetworkException($"Network {name} not found");
        }

        return network.ToDetailedDto();
    }

    public Task UpdateAsync(string name, NetworkUpdateRequest request)
    {
        throw new NotImplementedException();
    }
}   
