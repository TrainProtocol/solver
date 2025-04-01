using Temporalio.Activities;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Core.Workflows.Activities;

public abstract class BlockchainActivitiesBase(INetworkRepository networkRepository) : IBlockchainActivities
{
    protected abstract Task<string> GetCachedNonceAsync(NextNonceRequest request);
    protected abstract string FormatAddress(AddressRequest request);
    protected abstract bool ValidateAddress(AddressRequest request);

    [Activity] public abstract Task<string> GetNextNonceAsync(NextNonceRequest request);
    [Activity] public abstract Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);
    [Activity] public abstract Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);
    [Activity] public abstract Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
    [Activity] public abstract Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
    [Activity] public abstract Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);
    [Activity] public abstract Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);
    [Activity] public abstract Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);
    [Activity] public virtual async Task<string> GetSpenderAddressAsync(SpenderAddressRequest request)
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
