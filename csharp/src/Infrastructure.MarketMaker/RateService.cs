using System.Collections.Concurrent;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions;

namespace Train.Solver.Infrastructure.MarketMaker;

public class RateService : IRateService
{
    private readonly BinanceRestClient _binanceClient;
    private static readonly ConcurrentDictionary<string, (DateTime Timestamp, List<decimal> Prices)> _priceCache = new();
    private static readonly ConcurrentDictionary<string, string> _tradingSymbolCache = new();

    public RateService()
    {
        _binanceClient = new BinanceRestClient();
    }

    public async Task<decimal> GetRateAsync(Route route)
    {
        if (route.DestinationToken.TokenGroupId != null
        && route.SourceToken.TokenGroupId != null && route.DestinationToken.TokenGroupId == route.SourceToken.TokenGroupId)
        {
            // Same asset
            return 1;
        }

        var tradingSymbol = await GetTradingSymbol(route.SourceToken.Asset, route.DestinationToken.Asset) ?? throw new Exception($"No trading symbol found for {route.SourceToken.Asset} and {route.DestinationToken.Asset}");
        var isBuying = IsBuyTrade(tradingSymbol, route.SourceToken.Asset, route.DestinationToken.Asset);

        var prices = await FetchPrices(tradingSymbol);
        decimal stdev = CalculateStandardDeviation(prices);
        decimal currentPrice = prices.Last();

        return isBuying ? currentPrice - stdev : 1 / (currentPrice + stdev);
    }

    static decimal CalculateStandardDeviation(List<decimal> prices)
    {
        if (prices == null || prices.Count == 0)
            throw new ArgumentException("Price list is empty.");

        decimal avg = prices.Average();
        decimal sumSquares = prices.Sum(p => (p - avg) * (p - avg));
        decimal stdev = (decimal)Math.Sqrt((double)(sumSquares / prices.Count));

        return stdev;
    }
    
    private async Task<List<decimal>> FetchPrices(string symbol)
    {
        if (_priceCache.TryGetValue(symbol, out var cachedData) && cachedData.Timestamp > DateTime.UtcNow.AddMinutes(-30))
        {
            return cachedData.Prices;
        }

        var klines = await _binanceClient.SpotApi.ExchangeData.GetKlinesAsync(symbol, Binance.Net.Enums.KlineInterval.ThirtyMinutes, limit: 5000);
        if (!klines.Success || klines.Data == null)
        {
            throw new Exception($"Failed to fetch prices for symbol {symbol}");
        }

        var prices = klines.Data.Select(kline => kline.ClosePrice).ToList();
        _priceCache[symbol] = (DateTime.UtcNow, prices);

        return prices;
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

        return symbol?.Name;
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
