namespace Train.Solver.Infrastructure.Abstractions;

public interface IPrivateKeyProvider
{
    Task<string> GetAsync(string publicKey);
    Task<string> SetAsync(string publicKey,string privateKey);
}
