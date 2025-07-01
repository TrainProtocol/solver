using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Treasury.Client.Client;

namespace Train.Solver.Infrastructure.Services;

public class TreasuryPrivateKeyProvider(ITreasuryClient client) : IPrivateKeyProvider
{
    public Task<string> GenerateAsync(NetworkType type)
    {
        throw new NotImplementedException();
    }

    public Task<string> SignAsync(string publicKey, string message)
    {
        throw new NotImplementedException();
    }
}
