using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Util.Extensions;
using Train.Solver.Util.Helpers;

namespace Train.Solver.Infrastructure.Services;

public class RouteService(
    IRouteRepository routeRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository) : IRouteService
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
                    IsTestnet = network.IsTestnet,
                    ChainId = network.ChainId,
                    DisplayName = network.DisplayName,
                    FeeType = network.FeeType,
                    ListingDate = network.CreatedDate,
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
        var routeResult = await GetActiveRouteAsync(request, amount: null);

        return routeResult is not null ? GetLimit(routeResult) : null;
    }

    public virtual Task<QuoteDto?> GetValidatedQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, GetLimit);

    public virtual Task<QuoteDto?> GetQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, validatelimit: null);

    private static LimitDto GetLimit(RouteWithFeesDto route)
    {
        var minBufferAmount = MinUsdAmount / route.Source.PriceInUsd;
        var totalFee = CalculateTotalFee(route, minBufferAmount);
        var minAmount = minBufferAmount + totalFee;

        return new LimitDto
        {
            MinAmount = minAmount.Truncate(route.Source.Precision),
            MinAmountInUsd = (minAmount * route.Source.PriceInUsd).Truncate(2),
            MaxAmount = route.MaxAmountInSource.Truncate(route.Source.Precision),
            MaxAmountInUsd = (route.MaxAmountInSource * route.Source.PriceInUsd).Truncate(2),
        };
    }

    private async Task<QuoteDto?> GetQuoteAsync(
        QuoteRequest request,
        Func<RouteWithFeesDto, LimitDto>? validatelimit)
    {
        var shouldValidateLimit = validatelimit is not null;

        var route = await GetActiveRouteAsync(
            request,
            amount: shouldValidateLimit ? request.Amount : null);

        if (route is null)
        {
            return null;
        }

        if (shouldValidateLimit)
        {
            var limit = validatelimit!(route);

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

        var totalFee = CalculateTotalFee(route, request.Amount);
        var receiveAmount = request.Amount - totalFee;

        var quote = new QuoteDto
        {
            ReceiveAmount = receiveAmount.Truncate(route.Destionation.Precision),
            TotalFee = totalFee,
            TotalFeeInUsd = totalFee * route.Source.PriceInUsd,
        };

        return quote;
    }

    private static decimal CalculateTotalFee(RouteWithFeesDto route, decimal amount)
    {
        decimal fixedFee = default;
        decimal percentageFee = default;

        if (route.Expenses is not null)
        {
            fixedFee += route.Expenses.ExpenseFeeInSource;
        }

        if (route.ServiceFee is not null)
        {
            fixedFee += route.ServiceFee.ServiceFeeInSource;
            percentageFee = amount * route.ServiceFee.ServiceFeePercentage / 100m;
        }

        var totalFee = fixedFee + percentageFee;

        return totalFee.Truncate(route.Source.Precision);
    }

    private async Task<RouteWithFeesDto?> GetActiveRouteAsync(
        SourceDestinationRequest request,
        decimal? amount)
    {
        var route = await routeRepository.GetAsync(
            request.SourceNetwork,
            request.SourceToken,
            request.DestinationNetwork,
            request.DestinationToken,
            amount);

        if (route is null)
        {
            return null;
        }

        var mappedRoute = route.ToWithFeesDto();

        var expenseFee = await CalculateExpenseFeeAsync(route);

        if (expenseFee is not null)
        {
            mappedRoute.Expenses = new ExpenseFeeDto
            {
                ExpenseFeeInSource = expenseFee.ExpenseFeeInSource,
            };
        }

        var serviceFee = await CalculateServiceFeeAsync(route);

        if (serviceFee is not null)
        {
            mappedRoute.ServiceFee = new ServiceFeeDto
            {
                ServiceFeeInSource = serviceFee.ServiceFeeInSource,
                ServiceFeePercentage = serviceFee.ServiceFeePercentage,
            };
        }

        return mappedRoute;
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