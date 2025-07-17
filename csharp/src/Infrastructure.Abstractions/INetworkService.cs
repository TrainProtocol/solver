using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions;

public class NetworkUpdateRequest
{
    public string DisplayName { get; set; } = null!;

    public TransactionFeeType FeeType { get; set; }

    public int FeePercentageIncrease { get; set; }
}

public interface INetworkService
{
    public Task<DetailedNetworkDto> GetAsync(string name);

    public Task<IEnumerable<DetailedNetworkDto>> GetAllAsync();

    public Task CreateAsync(DetailedNetworkDto network);

    public Task UpdateAsync(string name, NetworkUpdateRequest request);
}
