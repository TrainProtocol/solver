using Refit;
using Train.Solver.Infrastrucutre.Secret.Treasury.Models;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Client;

public interface ITreasuryClient
{
    [Post("/api/treasury/{networkType}/sign")]
    Task<IApiResponse<SignTransactionResponseModel>> SignTransactionAsync(
        string networkType,
        [Body] SignTransactionRequestModel request);

    [Post("/api/treasury/{networkType}/generate")]
    Task<IApiResponse<GenerateAddressResponseModel>> GenerateAddressAsync(string networkType);
}
