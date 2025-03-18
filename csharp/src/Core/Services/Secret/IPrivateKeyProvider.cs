namespace Train.Solver.Core.Services.Secret;

public interface IPrivateKeyProvider
{
    Task<string> GetAsync(string publicKey);
    Task<string> SetAsync(string publicKey,string privateKey);
}
