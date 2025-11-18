using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ISignerAgentRepository
{
    public Task<SignerAgent?> GetAsync(string name);

    public Task<IEnumerable<SignerAgent>> GetAllAsync();

    public Task<SignerAgent?> CreateAsync(CreateSignerAgentRequest request);

    public Task DeleteAsync(string name);
}
