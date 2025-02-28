using FluentResults;
using Flurl;
using Flurl.Http;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Services;

public class TokenMarketPriceService
{
    private const string CoinGeckoBaseUrl = "https://api.coingecko.com/api";

    public virtual async Task<Result<Dictionary<string, TokenMarketPriceResponse>>> GetCoingeckoPricesAsync(string symbolsRaw)
    {
        var coinGeckoResponse = await CoinGeckoBaseUrl
            .AppendPathSegment("v3/simple/price")
            .SetQueryParam("ids", symbolsRaw)
            .SetQueryParam("vs_currencies", "usd")
            .AllowAnyHttpStatus()
            .GetAsync();

        if (!coinGeckoResponse.ResponseMessage.IsSuccessStatusCode)
        {
            var responseMessage = await coinGeckoResponse.GetStringAsync();

            return Result.Fail($"Error acquired while trying to get assets' prices. Status code: {coinGeckoResponse.ResponseMessage.StatusCode}. Message: {responseMessage}");
        }

        var coinGeckoTokens = await coinGeckoResponse.GetJsonAsync<Dictionary<string, TokenMarketPriceResponse>>();

        if (coinGeckoTokens == null)
        {
            Result.Fail($"Error acquired while trying to get assets' prices.");
        }

        return Result.Ok(coinGeckoTokens!);
    }
}
