using Refit;
using Train.Solver.Infrastructure.Treasury.Client.Models;

namespace Train.Solver.Infrastructure.Treasury.Client;

public interface ITreasuryClient
{
    [Post("/treasury/{networkType}/sign")]
    Task<IApiResponse<SignResponseModel>> SignAsync(
        string networkType,
        [Body] SignRequestModel request);

    [Post("/treasury/{networkType}/generate")]
    Task<IApiResponse<GenerateResponseModel>> GenerateAsync(string networkType);
}
