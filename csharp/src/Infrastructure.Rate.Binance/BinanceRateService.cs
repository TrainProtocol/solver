using System.Collections.Concurrent;
using Binance.Net.Clients;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Rate.Binance;

public class BinanceRateService : IRateProvider
{
    private readonly BinanceRestClient _binanceClient = new();
    private static readonly ConcurrentDictionary<string, string> _tradingSymbolCache = new();

    public string ProviderName => Constants.ProviderName;

    public async Task<decimal> GetRateAsync(RouteDto route)
    {
        var tradingSymbol = await GetTradingSymbol(route.Source.Token.Symbol, route.Destination.Token.Symbol);

        // direct traiding pair found
        if (tradingSymbol != null)
        {
            // Direct trading pair exists
            var isBuying = IsBuyTrade(tradingSymbol, route.Source.Token.Symbol, route.Destination.Token.Symbol);
            var price = await GetLastPriceAsync(tradingSymbol);

            return isBuying ? price : 1 / price;
        }
        // Use USDT as an intermediary
        else 
        {

            var sourceToUsdtPair = await GetTradingSymbol(route.Source.Token.Symbol, "USDT");
            var destinationToUsdtPair = await GetTradingSymbol(route.Destination.Token.Symbol, "USDT");

            if (sourceToUsdtPair == null || destinationToUsdtPair == null)
            {
                throw new Exception($"No valid trading pairs found for {route.Source.Token.Symbol} or {route.Destination.Token.Symbol} with USDT.");
            }

            var sourceToUsdtPrice = await GetLastPriceAsync(sourceToUsdtPair);
            var destinationToUsdtPrice = await GetLastPriceAsync(destinationToUsdtPair);

            return sourceToUsdtPrice / destinationToUsdtPrice;

        }
    }

    private async Task<decimal> GetLastPriceAsync(string symbol)
    {

        var ticker = await _binanceClient.SpotApi.ExchangeData.GetPriceAsync(symbol);
        if (!ticker.Success || ticker.Data == null)
        {
            throw new Exception($"Failed to fetch price for symbol {symbol}");
        }
        return ticker.Data.Price;
    }

    private async Task<string> GetTradingSymbol(string sourceToken, string destinationToken)
    {
        string cacheKey = $"{sourceToken}-{destinationToken}";
        if (_tradingSymbolCache.TryGetValue(cacheKey, out var cachedSymbol))
        {
            return cachedSymbol;
        }

        var exchangeInfo = await _binanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
        if (!exchangeInfo.Success || exchangeInfo.Data == null)
        {
            throw new Exception("Failed to fetch exchange information.");
        }

        string directPair1 = sourceToken + destinationToken;
        string directPair2 = destinationToken + sourceToken;

        var symbol = exchangeInfo.Data.Symbols.FirstOrDefault(s =>
            s.Name.Equals(directPair1, StringComparison.OrdinalIgnoreCase) ||
            s.Name.Equals(directPair2, StringComparison.OrdinalIgnoreCase));

        if (symbol != null)
        {
            _tradingSymbolCache[cacheKey] = symbol.Name;
        }

        return symbol?.Name ?? throw new Exception($"No trading pair found for {sourceToken} and {destinationToken}");
    }

    private static bool IsBuyTrade(string symbol, string sourceToken, string destinationToken)
    {
        // example SOL/ETH

        // If input is SOL, it means I want ETH, so I am selling SOL
        if (symbol.StartsWith(destinationToken))
        {
            return false;
        }

        if (symbol.StartsWith(sourceToken))
        {
            return true;
        }

        throw new Exception($"Invalid trading symbol {symbol}");
    }
}
