using Flurl;
using Flurl.Http;
using Train.Solver.Core.Data;

namespace Train.Solver.Core.Services.TokenPrice.Coingecko;

public class CoingeckoTokenPriceService(SolverDbContext dbContext) : TokenPriceServiceBase(dbContext)
{
    private const string CoinGeckoBaseUrl = "https://api.coingecko.com/api";

    public override async Task<Dictionary<string, decimal>> GetPricesAsync(params string[] tokenSymbols)
    {
        if (tokenSymbols.Length == 0)
        {
            return [];
        }

        var coinGeckoResponse = await CoinGeckoBaseUrl
            .AppendPathSegment("v3/simple/price")
            .SetQueryParam("ids", string.Join(',', tokenSymbols))
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
