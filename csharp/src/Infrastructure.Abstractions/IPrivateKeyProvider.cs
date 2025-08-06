using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions;

public interface IPrivateKeyProvider
{
    Task<string> GenerateAsync(string signerAgentUrl, NetworkType type);

    Task<string> SignAsync(string signerAgentUrl, NetworkType type, string publicKey, string message);
}
