using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public abstract class BlockchainActivitiesBase() : IBlockchainActivities
{
    protected abstract string FormatAddress(string request);
    protected abstract bool ValidateAddress(string request);

    [Activity] public abstract Task<string> GetNextNonceAsync(NextNonceRequest request);
    [Activity] public abstract Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);
    [Activity] public abstract Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);
    [Activity] public abstract Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
    [Activity] public abstract Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
    [Activity] public abstract Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);
    [Activity] public abstract Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);
    [Activity] public abstract Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request); 
}
