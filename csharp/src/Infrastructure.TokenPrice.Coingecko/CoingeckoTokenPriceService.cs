using Flurl;
using Flurl.Http;
using Train.Solver.Infrastructure.Abstractions;

namespace Train.Solver.Infrastructure.TokenPrice.Coingecko;

public class CoingeckoTokenPriceService : ITokenPriceService
{
    private const string CoinGeckoBaseUrl = "https://api.coingecko.com/api";

    public async Task<Dictionary<string, decimal>> GetPricesAsync(string[] externalIds)
    {
        var coinGeckoResponse = await CoinGeckoBaseUrl
            .AppendPathSegment("v3/simple/price")
            .SetQueryParam("ids", string.Join(',', externalIds))
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
