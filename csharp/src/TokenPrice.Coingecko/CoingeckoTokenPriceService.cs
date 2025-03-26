using Flurl;
using Flurl.Http;
using Train.Solver.Core.Repositories;
using Train.Solver.Core.Services;

namespace Train.Solver.TokenPrice.Coingecko;

public class CoingeckoTokenPriceService(INetworkRepository networkRepository) : ITokenPriceService
{
    private const string CoinGeckoBaseUrl = "https://api.coingecko.com/api";

    public async Task<Dictionary<string, decimal>> GetPricesAsync()
    {
        var tokens = await networkRepository.GetTokensAsync();
        var tokenExternalIds = tokens.Select(x => x.TokenPrice.ExternalId).ToList();

        var coinGeckoResponse = await CoinGeckoBaseUrl
            .AppendPathSegment("v3/simple/price")
            .SetQueryParam("ids", string.Join(',', tokenExternalIds))
            .SetQueryParam("vs_currencies", "usd")
            .AllowAnyHttpStatus()
            .GetAsync();

        if (!coinGeckoResponse.ResponseMessage.IsSuccessStatusCode)
        {
            var responseMessage = await coinGeckoResponse.GetStringAsync();

            throw new Exception(
                $"Error acquired while trying to get assets' prices. Status code: {coinGeckoResponse.ResponseMessage.StatusCode}. Message: {responseMessage}");
        }

        var coinGeckoTokens = await coinGeckoResponse.GetJsonAsync<Dictionary<string, TokenMarketPriceResponse>>();

        if (coinGeckoTokens == null)
        {
            throw new Exception($"Error acquired while trying to get assets' prices.");
        }

        return coinGeckoTokens.ToDictionary(x => x.Key, y => y.Value.Usd);
    }
}
