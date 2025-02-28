using FluentResults;

namespace Train.Solver.Core.Secret;

public interface IPrivateKeyProvider
{
    Task<Result<string>> GetAsync(string publicKey);
    Task<Result<string>> SetAsync(string publicKey,string privateKey);
}
