using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Util.Extensions;
using Train.Solver.Util.Helpers;
using Nethereum.Util;
using Train.Solver.Util;

namespace Train.Solver.Infrastructure.MarketMaker;

public class RouteService(
    IRouteRepository routeRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IRateService rateService) : IRouteService
{
    public const decimal MinUsdAmount = 0.69m;

    public async Task<IEnumerable<DetailedNetworkDto>?> GetSourcesAsync(string? networkName, string? token)
    {
        return await GetReachablePointsAsync(
            fromSrcToDest: false,
            networkName: networkName,
            asset: token);
    }

    public async Task<IEnumerable<DetailedNetworkDto>?> GetDestinationsAsync(string? networkName, string? token)
    {
        return await GetReachablePointsAsync(
           fromSrcToDest: true,
           networkName: networkName,
           asset: token);
    }

    private async Task<IEnumerable<DetailedNetworkDto>?> GetReachablePointsAsync(
        bool fromSrcToDest,
        string? networkName,
        string? asset)
    {
        if (!string.IsNullOrEmpty(asset) && string.IsNullOrEmpty(networkName)
            || !string.IsNullOrEmpty(networkName) && string.IsNullOrEmpty(asset))
        {
            throw new Exception($"{(fromSrcToDest ? "Source" : "Destination")} network and token should be provided");
        }

        Token? reuqestedPoint = null;

        if (!string.IsNullOrEmpty(asset) && !string.IsNullOrEmpty(networkName))
        {
            reuqestedPoint = await networkRepository.GetTokenAsync(networkName, asset);

            if (reuqestedPoint == null)
            {
                return null;
            }
        }

        var reachablePoints = await routeRepository.GetReachablePointsAsync(
            [RouteStatus.Active],
            fromSrcToDest,
            reuqestedPoint?.Id);

        if (reachablePoints == null || !reachablePoints.Any())
        {
            return null;
        }

        var tokens = await networkRepository.GetTokensAsync(reachablePoints.ToArray());

        var networksWithAmounts = tokens
            .GroupBy(x => x.Network.Name)
            .Select(x =>
            {
                var network = x.First().Network;
                var networkWithTokens = new DetailedNetworkDto
                {
                    Name = x.Key,
                    Type = network.Type,
                    ChainId = network.ChainId,
                    DisplayName = network.DisplayName,
                    HTLCNativeContractAddress = network.HTLCNativeContractAddress,
                    HTLCTokenContractAddress = network.HTLCTokenContractAddress,
                    Tokens = x.Select(x => x.ToDto()),
                    Nodes = network.Nodes.Select(x => x.ToDto()),
                    NativeToken = network.NativeToken?.ToDto(),
                };

                return networkWithTokens;

            });

        return networksWithAmounts;
    }

    public virtual async Task<LimitDto?> GetLimitAsync(SourceDestinationRequest request)
    {
        var route = await routeRepository.GetAsync(
            request.SourceNetwork,
            request.SourceToken,
            request.DestinationNetwork,
            request.DestinationToken,
            null);

        return route is not null ? await GetLimitAsync(route) : null;
    }

    public virtual Task<QuoteWithSolverDto?> GetValidatedQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, GetLimitAsync);

    public virtual Task<QuoteWithSolverDto?> GetQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, validatelimit: null);

    private async Task<LimitDto> GetLimitAsync(Route route)
    {
        var minBufferAmount = TokenUnitConverter.ToBaseUnits(
            MinUsdAmount / route.SourceToken.TokenPrice.PriceInUsd,
            route.SourceToken.Decimals);

        var totalFee = await CalculateTotalFeeAsync(route, minBufferAmount);
        var minAmount = minBufferAmount + totalFee;

        return new LimitDto
        {
            MinAmount = minAmount.ToString(),
            MinAmountInUsd = (TokenUnitConverter.FromBaseUnits(minAmount, route.SourceToken.Decimals) * route.SourceToken.TokenPrice.PriceInUsd).Truncate(2),
            MaxAmount = TokenUnitConverter.ToBaseUnits(route.MaxAmountInSource, route.SourceToken.Decimals).ToString(),
            MaxAmountInUsd = (route.MaxAmountInSource * route.SourceToken.TokenPrice.PriceInUsd).Truncate(2),
        };
    }

    private async Task<QuoteWithSolverDto?> GetQuoteAsync(
        QuoteRequest request,
        Func<Route, Task<LimitDto>>? validatelimit)
    {
        var shouldValidateLimit = validatelimit is not null;

        var route = await routeRepository.GetAsync(
            request.SourceNetwork,
            request.SourceToken,
            request.DestinationNetwork,
            request.DestinationToken,
            shouldValidateLimit ? request.Amount : null);

        if (route is null)
        {
            return null;
        }

        if (shouldValidateLimit)
        {
            var limit = await validatelimit!(route);

            if (BigInteger.Parse(request.Amount) < BigInteger.Parse(limit.MinAmount))
            {
                throw new ArgumentException($"Amount is less than min amount {limit.MinAmount}.",
                    nameof(request.Amount));
            }

            if (BigInteger.Parse(request.Amount) > BigInteger.Parse(limit.MaxAmount))
            {
                throw new ArgumentException($"Amount is greater than max amount {limit.MaxAmount}.",
                    nameof(request.Amount));
            }
        }

        var swapRate = await rateService.GetRateAsync(route);
        var amount = BigInteger.Parse(request.Amount);
        var totalFee = await CalculateTotalFeeAsync(route, amount);
        var actualAmountToSwap = amount - totalFee;
        var receiveAmount = actualAmountToSwap.ConvertTokenAmount(swapRate, route.SourceToken.Decimals, route.DestinationToken.Decimals);

        var solverAddressInSource = await networkRepository.GetSolverAccountAsync(route.SourceToken.Network.Name);

        if (string.IsNullOrEmpty(solverAddressInSource))
        {
            throw new Exception($"Solver account not found for network {route.SourceToken.Network.Name}");
        }

        var quote = new QuoteWithSolverDto
        {
            SourceAmount = request.Amount,
            SourceAmountInUsd = BigInteger.Parse(request.Amount).ToUsd(route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals).Truncate(2),
            ReceiveAmount = receiveAmount.ToString(),
            ReceiveAmountInUsd = receiveAmount.ToUsd(route.DestinationToken.TokenPrice.PriceInUsd, route.DestinationToken.Decimals),
            TotalFee = totalFee.ToString(),
            TotalFeeInUsd = totalFee.ToUsd(route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals),
            SolverAddressInSource = solverAddressInSource,
            NativeContractAddressInSource = route.SourceToken.Network.HTLCNativeContractAddress,
            TokenContractAddressInSource = route.SourceToken.Network.HTLCTokenContractAddress,
        };

        return quote;
    }

    private async Task<BigInteger> CalculateTotalFeeAsync(Route route, BigInteger amount)
    {
        BigInteger fixedFee = default;
        BigInteger percentageFee = default;

        var expenseFee = await CalculateExpenseFeeAsync(route);

        if (expenseFee is not null)
        {
            fixedFee += BigInteger.Parse(expenseFee.ExpenseFeeInSource);
        }

        var serviceFee = await CalculateServiceFeeAsync(route);

        if (serviceFee is not null)
        {
            fixedFee += BigInteger.Parse(serviceFee.ServiceFeeInSource);
            percentageFee = amount.PercentOf(serviceFee.ServiceFeePercentage);
        }

        var totalFee = fixedFee + percentageFee;

        return totalFee;
    }

    private async Task<ServiceFeeDto> CalculateServiceFeeAsync(
        Route route)
    {
        var serviceFees = await feeRepository.GetServiceFeesAsync();

        var fee = new ServiceFeeDto()
        {
             ServiceFeeInSource = BigInteger.Zero.ToString(),
             ServiceFeePercentage = default
        };

        if (serviceFees.Any())
        {
            var serviceFee = MatchServiceFee(
                serviceFees,
                new SourceDestinationRequest()
                {
                    SourceNetwork = route.SourceToken.Network.Name,
                    SourceToken = route.SourceToken.Asset,
                    DestinationNetwork = route.DestinationToken.Network.Name,
                    DestinationToken = route.DestinationToken.Asset,
                });

            if (serviceFee != null)
            {
                fee.ServiceFeePercentage = serviceFee.FeePercentage;
                fee.ServiceFeeInSource = TokenUnitConverter.ToBaseUnits(
                    serviceFee.FeeInUsd / route.SourceToken.TokenPrice.PriceInUsd,
                    route.SourceToken.Decimals).ToString();

            }
        }

        return fee;
    }

    private static ServiceFee? MatchServiceFee(
        List<ServiceFee> feeSettings,
        SourceDestinationRequest request)
    {
        if (feeSettings.Any())
        {
            // Helper methods to check matches and nullity
            bool matches(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

            // Consolidated match checkers
            bool matchesSource(ServiceFee x) => matches(x.SourceNetwork, request.SourceNetwork) &&
                                                matches(x.SourceAsset, request.SourceToken);

            bool matchesDestination(ServiceFee x) => matches(x.DestinationNetwork, request.DestinationNetwork) &&
                                                     matches(x.DestinationAsset, request.DestinationToken);

            bool isSourceNull(ServiceFee x) => x.SourceNetwork is null && x.SourceAsset is null;
            bool isDestinationNull(ServiceFee x) => x.DestinationNetwork is null && x.DestinationAsset is null;

            // Specific scenario matchers using the helper and consolidated methods
            bool matchExactSourceDestNetworkAssets(ServiceFee x) => matchesSource(x) && matchesDestination(x);
            bool matchSourceAssetNullOthers(ServiceFee x) => matchesSource(x) && isDestinationNull(x);
            bool matchDestAssetNullOthers(ServiceFee x) => isSourceNull(x) && matchesDestination(x);

            bool matchSourceNetworkNullOthers(ServiceFee x) => matches(x.SourceNetwork, request.SourceNetwork) &&
                                                               isDestinationNull(x) && x.SourceAsset is null;

            bool matchDestNetworkNullOthers(ServiceFee x) => isSourceNull(x) &&
                                                             matches(x.DestinationNetwork,
                                                                 request.DestinationNetwork) &&
                                                             x.DestinationAsset is null;

            bool matchGlobalFee(ServiceFee x) => isSourceNull(x) && isDestinationNull(x);

            var matchers = new Func<ServiceFee, bool>[]
            {
                matchExactSourceDestNetworkAssets,
                matchSourceAssetNullOthers,
                matchDestAssetNullOthers,
                matchSourceNetworkNullOthers,
                matchDestNetworkNullOthers,
                matchGlobalFee
            };

            foreach (var matcher in matchers)
            {
                var feeSetting = feeSettings.FirstOrDefault(matcher);
                if (feeSetting != null)
                {
                    return feeSetting;
                }
            }
        }

        return null;
    }

    private async Task<ExpenseFeeDto?> CalculateExpenseFeeAsync(Route route)
    {
        var expenses = await feeRepository.GetExpensesAsync();


        var filterredExpenses = expenses
            .Where(x =>
                x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCLock
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCAddLockSig
                || x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCRedeem
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCRedeem);

        ExpenseFeeDto? fee = null;

        if (filterredExpenses.Any())
        {
            fee = new();

            foreach (var transactionCompletionDetail in filterredExpenses)
            {
                var expenseFeeAmountInUsd = BigInteger.Parse(transactionCompletionDetail.FeeAmount).ToUsd(
                    transactionCompletionDetail.FeeToken.TokenPrice.PriceInUsd,
                    transactionCompletionDetail.FeeToken.Decimals);

                fee.ExpenseFeeInSource += TokenUnitConverter.ToBaseUnits(expenseFeeAmountInUsd / route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals);
            }
        }

        return fee;
    }
}