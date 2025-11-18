using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFSignerAgentRepository(SolverDbContext dbContext) : ISignerAgentRepository
{
    public async Task<SignerAgent?> CreateAsync(CreateSignerAgentRequest request)
    {
        var signerAgentExists = await dbContext.SignerAgents.AnyAsync(x => x.Name == request.Name);

        if (signerAgentExists)
        {
            return null;
        }

        var signerAgent = new SignerAgent
        {
            Name = request.Name,
            Url = request.Url,
            SupportedTypes = request.SupportedTypes
        };

        dbContext.SignerAgents.Add(signerAgent);
        await dbContext.SaveChangesAsync();

        return signerAgent;
    }

    public async Task DeleteAsync(string name)
    {
        await dbContext.SignerAgents
            .Where(x => x.Name == name)
            .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<SignerAgent>> GetAllAsync()
    {
        var signerAgents = await dbContext.SignerAgents.ToListAsync();
        return signerAgents;
    }

    public async Task<SignerAgent?> GetAsync(string name)
    {
        var signerAgent = await dbContext.SignerAgents.FirstOrDefaultAsync(x => x.Name == name);
        return signerAgent;
    }

}
