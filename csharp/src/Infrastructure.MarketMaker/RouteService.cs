using Microsoft.Extensions.Options;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Util.Extensions;
using Train.Solver.Util.Helpers;
using Train.Solver.Util.Shared.Options;

namespace Train.Solver.Infrastructure.MarketMaker;

public class RouteService(
    IRouteRepository routeRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IRateService rateService,
    IOptions<ExpenseFeeOptions> options) : IRouteService
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
                    Logo = LogoHelpers.BuildGithubLogoUrl(network.Logo),
                    Name = x.Key,
                    Type = network.Type,
                    AccountExplorerTemplate = network.AccountExplorerTemplate,
                    TransactionExplorerTemplate = network.TransactionExplorerTemplate,
                    ChainId = network.ChainId,
                    DisplayName = network.DisplayName,
                    Contracts = network.Contracts.Select(x => x.ToDto()),
                    Tokens = x.Select(x => x.ToDetailedDto()),
                    ManagedAccounts = network.ManagedAccounts.Select(x => x.ToDto()),
                    Nodes = network.Nodes.Where(x=>x.Type == NodeType.Public).Select(x=> x.ToDto()),
                };

                if (network.NativeToken != null)
                {
                    networkWithTokens.NativeToken = network.NativeToken.ToDetailedDto();
                }

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
        var minBufferAmount = MinUsdAmount / route.SourceToken.TokenPrice.PriceInUsd;
        var totalFee = await CalculateTotalFeeAsync(route, minBufferAmount);
        var minAmount = minBufferAmount + totalFee;

        return new LimitDto
        {
            MinAmount = minAmount.Truncate(route.SourceToken.Precision),
            MinAmountInUsd = (minAmount * route.SourceToken.TokenPrice.PriceInUsd).Truncate(2),
            MaxAmount = route.MaxAmountInSource.Truncate(route.SourceToken.Precision),
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

            if (request.Amount < limit.MinAmount)
            {
                throw new ArgumentException($"Amount is less than min amount {limit.MinAmount}.",
                    nameof(request.Amount));
            }

            if (request.Amount > limit.MaxAmount)
            {
                throw new ArgumentException($"Amount is greater than max amount {limit.MaxAmount}.",
                    nameof(request.Amount));
            }
        }

        var swapRate = await rateService.GetRateAsync(route);
        var totalFee = await CalculateTotalFeeAsync(route, request.Amount);
        var actualAmountToSwap = request.Amount - totalFee;
        var receiveAmount = actualAmountToSwap * swapRate;

        var quote = new QuoteWithSolverDto
        {
            SourceAmount = request.Amount.Truncate(route.SourceToken.Precision),
            SourceAmountInUsd = request.Amount * route.SourceToken.TokenPrice.PriceInUsd,
            ReceiveAmount = receiveAmount.Truncate(route.DestinationToken.Precision),
            ReceiveAmountInUsd = receiveAmount * route.DestinationToken.TokenPrice.PriceInUsd,
            TotalFee = totalFee,
            TotalFeeInUsd = totalFee * route.SourceToken.TokenPrice.PriceInUsd,
            SolverAddressInSource = route.SourceToken.Network.ManagedAccounts.FirstOrDefault()?.Address ?? string.Empty,
            NativeContractAddressInSource = route.SourceToken.Network.Contracts.FirstOrDefault(x=>x.Type == ContarctType.HTLCNativeContractAddress)?.Address ?? string.Empty,
            TokenContractAddressInSource = route.SourceToken.Network.Contracts.FirstOrDefault(x=>x.Type == ContarctType.HTLCTokenContractAddress)?.Address ?? string.Empty,
        };

        return quote;
    }

    private async Task<decimal> CalculateTotalFeeAsync(Route route, decimal amount)
    {
        decimal fixedFee = default;
        decimal percentageFee = default;

        var expenseFee = await CalculateExpenseFeeAsync(route);

        if (expenseFee is not null && !options.Value.DisableExpenseFee)
        {
            fixedFee += expenseFee.ExpenseFeeInSource;
        }

        var serviceFee = await CalculateServiceFeeAsync(route);

        if (serviceFee is not null)
        {
            fixedFee += serviceFee.ServiceFeeInSource;
            percentageFee = amount * serviceFee.ServiceFeePercentage / 100m;
        }

        var totalFee = fixedFee + percentageFee;

        return totalFee.Truncate(route.SourceToken.Precision);
    }

    private async Task<ServiceFeeDto> CalculateServiceFeeAsync(
        Route route)
    {
        var serviceFees = await feeRepository.GetServiceFeesAsync();

        var fee = new ServiceFeeDto();

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
                fee.ServiceFeeInSource =
                    (serviceFee.FeeInUsd / route.SourceToken.TokenPrice.PriceInUsd).Truncate(
                        route.SourceToken.Precision);
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
                var expenseFeeAmountInUsd = transactionCompletionDetail.FeeAmount *
                                            transactionCompletionDetail.FeeToken.TokenPrice.PriceInUsd;
                fee.ExpenseFeeInSource +=
                    (expenseFeeAmountInUsd / route.SourceToken.TokenPrice.PriceInUsd).Truncate(route.SourceToken
                        .Precision);
            }
        }

        return fee;
    }
}