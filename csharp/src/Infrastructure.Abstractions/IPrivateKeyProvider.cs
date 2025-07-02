using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions;

public interface IPrivateKeyProvider
{
    Task<string> GenerateAsync(NetworkType type);

    Task<string> SignAsync(NetworkType type, string publicKey, string message);
}
