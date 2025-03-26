namespace Train.Solver.Core.Abstractions;

public interface IPrivateKeyProvider
{
    Task<string> GetAsync(string publicKey);
    Task<string> SetAsync(string publicKey,string privateKey);
}
