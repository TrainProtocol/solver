using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ISignerAgentRepository
{
    public Task<SignerAgent?> GetAsync(string name);

    public Task<IEnumerable<SignerAgent>> GetAllAsync();

    public Task<SignerAgent?> CreateAsync(string name, string url, NetworkType[] supportedTypes);

    public Task DeleteAsync(string name);
}
