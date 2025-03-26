using Train.Solver.Core.Models;

namespace Train.Solver.Core.Workflows.Activities;

public interface IBlockchainActivities
{
    Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);

    Task EnsureSufficientBalanceAsync(SufficientBalanceRequest request);

    Task<string> GetSpenderAddressAsync(SpenderAddressRequest request);

    Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);

    Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);

    Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);

    Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);

    Task<string> GetReservedNonceAsync(ReservedNonceRequest request);

    Task<string> GetNextNonceAsync(NextNonceRequest request);

    Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);

    Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
}
