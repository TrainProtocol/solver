using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Core.Workflows.Activities;

public abstract class BlockchainActivitiesBase(
    INetworkRepository networkRepository,
    ISwapRepository swapRepository) : IBlockchainActivities
{
    protected abstract Task<string> GetCachedNonceAsync(NextNonceRequest request);
    protected abstract string FormatAddress(AddressRequest request);
    protected abstract bool ValidateAddress(AddressRequest request);

    public abstract Task<string> GetNextNonceAsync(NextNonceRequest request);
    public abstract Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);
    public abstract Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);
    public abstract Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
    public abstract Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
    public abstract Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);
    public abstract Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);
    public abstract Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);
    public virtual async Task<string> GetReservedNonceAsync(ReservedNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var id = Guid.Parse(request.ReferenceId!);

        var reservedNonce = await swapRepository.GetSwapTransactionReservedNonceAsync(id);

        if (reservedNonce is not null)
        {
            return reservedNonce.Nonce;
        }

        var nextNonce = await GetCachedNonceAsync(new NextNonceRequest { Address = request.Address, NetworkName = request.NetworkName });

        if (nextNonce == null)
        {
            throw new("Failed to get next nonce");
        }

        await swapRepository.CreateSwapTransactionReservedNonceAsync(request.NetworkName, id, nextNonce);

        return nextNonce;
    }
    public virtual async Task EnsureSufficientBalanceAsync(SufficientBalanceRequest request)
    {
        var balance = await GetBalanceAsync(
            new()
            {
                NetworkName = request.NetworkName,
                Address = request.Address,
                Asset = request.Asset,
            });

        if (request.Amount > balance.Amount)
        {
            throw new($"Insufficient balance on {request.Address}. Balance is less than {request.Amount}");
        }
    }
    public virtual async Task<string> GetSpenderAddressAsync(SpenderAddressRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var token = network.Tokens.FirstOrDefault(x => x.Asset == request.Asset);

        if (token is null)
        {
            throw new ArgumentNullException(nameof(token), $"Token {request.Asset} not found in {request.NetworkName}");
        }

        return string.IsNullOrEmpty(token.TokenContract) ?
            network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;
    }
}
