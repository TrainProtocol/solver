using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;

namespace Train.Solver.Infrastructure.Services;

public class WalletService(IPrivateKeyProvider privateKeyProvider, IWalletRepository walletRepository) : IWalletService
{
    public async Task<string> CreateAsync(CreateWalletRequest request)
    {
        var publicKey = await privateKeyProvider.GenerateAsync(request.Type);

        var wallet = await walletRepository.CreateAsync(
            request.Type,
            publicKey,
            request.Name);

        if (wallet == null)
        {
            throw new InvalidOperationException($"Failed to create wallet for {request.Type}");
        }

        return wallet.Address;
    }

    public Task<IEnumerable<string>> GetAllAsync(NetworkType type)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetAsync(NetworkType type)
    {
        throw new NotImplementedException();
    }
}
