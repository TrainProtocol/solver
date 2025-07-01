using Refit;
using Train.Solver.Infrastructure.Treasury.Client.Models;

namespace Train.Solver.Infrastructure.Treasury.Client.Client;

public interface ITreasuryClient
{
    [Post("/treasury/{networkType}/sign")]
    Task<IApiResponse<SignTransactionResponseModel>> SignTransactionAsync(
        string networkType,
        [Body] SignTransactionRequestModel request);

    [Post("/treasury/{networkType}/generate")]
    Task<IApiResponse<GenerateAddressResponseModel>> GenerateAddressAsync(string networkType);
}
