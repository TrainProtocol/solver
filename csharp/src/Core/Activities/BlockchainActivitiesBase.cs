using Microsoft.EntityFrameworkCore;
using Serilog;
using Train.Solver.Core.Data;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Activities;

public abstract class BlockchainActivitiesBase(
    SolverDbContext dbContext) : IBlockchainActivities
{
    protected abstract Task<string> GetPersistedNonceAsync(string networkName, string address);
    public abstract Task<string> GetNextNonceAsync(string networkName, string address);
    public abstract Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args);
    public abstract Task<string> GenerateAddressAsync(string networkName);
    public abstract Task<Fee> EstimateFeeAsync(string networkName, EstimateFeeRequest request);
    public abstract Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset);
    public abstract Task<TransactionModel> GetTransactionAsync(string network, string transactionId);
    public abstract Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock);
    public abstract Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName);
    public abstract Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request);
    public abstract string FormatAddress(string address);
    public abstract bool ValidateAddress(string address);

    public virtual async Task<string> GetReservedNonceAsync(
        string networkName,
        string address,
        string referenceId)
    {
        var network = await dbContext.Networks
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var reservedNonce = await dbContext.ReservedNonces
             .Include(x => x.Network)
             .FirstOrDefaultAsync(x =>
                 x.NetworkId == network.Id
                 && x.ReferenceId == referenceId);

        if (reservedNonce is not null)
        {
            return reservedNonce.Nonce;
        }

        var nextNonceResult = await GetPersistedNonceAsync(
            networkName,
            address);

        if (nextNonceResult is not null)
        {
            return nextNonceResult;
        }

        reservedNonce = new()
        {
            ReferenceId = referenceId,
            Nonce = nextNonceResult.ToString(),
            NetworkId = network.Id,
        };

        dbContext.Add(reservedNonce);

        await dbContext.SaveChangesAsync();

        return reservedNonce.Nonce;
    }

    public virtual async Task EnsureSufficientBalanceAsync(
        string networkName,
        string address,
        string asset,
        decimal amount)
    {
        var balance = await GetBalanceAsync(networkName, address, asset);

        if (amount > balance.Amount)
        {
            Log.Warning($"Insufficient {asset} funds in {networkName} {address}");
            throw new($"Insufficient balance on {address}. Balance is less than {amount}");
        }
    }

    public virtual async Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        var currency = await dbContext.Tokens
            .Include(x => x.Network).ThenInclude(network => network.DeployedContracts)
            .SingleOrDefaultAsync(x =>
                 x.Asset.ToUpper() == asset.ToUpper()
                 && x.Network.Name.ToUpper() == networkName.ToUpper());

        if (currency == null)
        {
            throw new("Invalid currency");
        }

        return string.IsNullOrEmpty(currency.TokenContract) ?
            currency.Network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : currency.Network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;
    }
}
