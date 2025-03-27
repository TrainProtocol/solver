using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;
using Train.Solver.Core.Extensions;

namespace Train.Solver.Core.Services;

public class RouteService(
    IRouteRepository routeRepository,
    INetworkRepository tokenRepository,
    IFeeRepository feeRepository) : IRouteService
{
    public const decimal MinUsdAmount = 0.69m;

    public virtual async Task<IEnumerable<Token>?> GetReachablePointsAsync(
        bool fromSrcToDest,
        string? networkName,
        string? asset)
    {
        var requests = new List<FindReachableTokenRequest>();

        if (!string.IsNullOrEmpty(asset) && !string.IsNullOrEmpty(networkName))
        {
            var token = await tokenRepository.GetTokenAsync(networkName, asset);

            if (token is null)
            {
                throw new Exception($"Token {asset} not found in network {networkName}");
            }

            requests.Add(new()
            {
                Asset = asset,
                Network = networkName,
                FromSource = fromSrcToDest,
            });
        }
        else if (!string.IsNullOrEmpty(asset) && string.IsNullOrEmpty(networkName)
            || !string.IsNullOrEmpty(networkName) && string.IsNullOrEmpty(asset))
        {
            throw new Exception($"{(fromSrcToDest ? "Source" : "Destination")} network and token should be provided");
        }

        var reachableTokens = new List<Token>();

        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);

        if (!requests.Any())
        {
            reachableTokens.AddRange(
                routes.Select(x => fromSrcToDest ? x.DestinationToken : x.SourceToken).DistinctBy(x => x.Id));
        }
        else
        {
            foreach (var request in requests)
            {
                if (!string.IsNullOrEmpty(request.Network) && !string.IsNullOrEmpty(request.Asset))
                {
                    var token = routes
                        .Select(x => request.FromSource ? x.SourceToken : x.DestinationToken)
                        .DistinctBy(x => x.Id)
                        .SingleOrDefault(x =>
                            x.Network.Name.ToUpper() == request.Network.ToUpper()
                            && x.Asset.ToUpper() == request.Asset.ToUpper());

                    if (token is null)
                    {
                        continue;
                    }

                    var reachableRoutes = routes.Where(x =>
                        request.FromSource ? x.SourceTokenId == token.Id : x.DestinationTokenId == token.Id);

                    reachableTokens
                        .AddRange(reachableRoutes
                            .Select(x => fromSrcToDest ? x.DestinationToken : x.SourceToken)
                            .DistinctBy(x => x.Id));
                }
                else if (string.IsNullOrEmpty(request.Network) && string.IsNullOrEmpty(request.Asset))
                {
                    reachableTokens
                        .AddRange(routes
                            .Select(x => fromSrcToDest ? x.DestinationToken : x.SourceToken)
                            .DistinctBy(x => x.Id));
                }
            }

            if (!reachableTokens.Any())
            {
                return null;
            }
        }

        return reachableTokens.AsEnumerable();
    }

    public virtual async Task<LimitModel?> GetLimitAsync(SourceDestinationRequest request)
    {
        var routeResult = await GetActiveRouteAsync(request, amount: null);

        return routeResult is not null ? GetLimit(routeResult) : null;
    }

    public virtual Task<QuoteModel?> GetValidatedQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, GetLimit);

    public virtual Task<QuoteModel?> GetQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, validatelimit: null);
      
    private static LimitModel? GetLimit(RouteWithFeesModel route)
    {
        var minBufferAmount = MinUsdAmount / route.Source.UsdPrice;

        var fixedFee = route.ExpenseInSource + route.ServiceFeeInSource;
        var percentageFee = (minBufferAmount + fixedFee) * route.ServiceFeePercentage / 100m;
        var totalFee = fixedFee + percentageFee;
        var minAmount = minBufferAmount + totalFee;

        return new LimitModel
        {
            Route = route,
            MinAmount = minAmount.Truncate(route.Source.Precision),
            MaxAmount = route.MaxAmountInSource.Truncate(route.Source.Precision),
        };
    }

    private async Task<QuoteModel?> GetQuoteAsync(
        QuoteRequest request,
        Func<RouteWithFeesModel, LimitModel>? validatelimit)
    {
        var shouldValidateLimit = validatelimit is not null;

        var routeResult = await GetActiveRouteAsync(
            request,
            amount: shouldValidateLimit ? request.Amount : null);

        if (routeResult is null)
        {
            return null;
        }

        if (shouldValidateLimit)
        {
            var limit = validatelimit!(routeResult);

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

        var fixedFee = routeResult.ExpenseInSource + routeResult.ServiceFeeInSource;
        var percentageFee = request.Amount * routeResult.ServiceFeePercentage / 100m;
        var totalFee = fixedFee + percentageFee;
        var receiveAmount = request.Amount - totalFee;

        var quote = new QuoteModel
        {
            Route = routeResult,
            ReceiveAmount = receiveAmount.Truncate(routeResult.Destionation.Precision),
            TotalFee = totalFee
        };

        return quote;
    }

    private async Task<RouteWithFeesModel?> GetActiveRouteAsync(
        SourceDestinationRequest request,
        decimal? amount)
    {
        var route = await routeRepository.GetAsync(
            request.SourceNetwork,
            request.SourceToken,
            request.DestinationNetwork,
            request.DestinationToken,
            amount);

        var mappedRoute = new RouteWithFeesModel()
        {
            Id = route.Id,
            MaxAmountInSource = route.MaxAmountInSource,
            Status = route.Status,
            Source = new TokenModel
            {
                Id = route.SourceToken.Id,
                NetworkName = route.SourceToken.Network.Name,
                Asset = route.SourceToken.Asset,
                Precision = route.SourceToken.Precision,
                IsNative = route.SourceToken.IsNative,
                UsdPrice = route.SourceToken.TokenPrice.PriceInUsd,
            },
            Destionation = new TokenModel
            {
                Id = route.DestinationToken.Id,
                NetworkName = route.DestinationToken.Network.Name,
                Asset = route.DestinationToken.Asset,
                Precision = route.DestinationToken.Precision,
                IsNative = route.DestinationToken.IsNative,
                UsdPrice = route.DestinationToken.TokenPrice.PriceInUsd,
            }
        };

        var expenseFee = await CalculateExpenseFeeAsync(route);

        if (expenseFee is not null)
        {
            mappedRoute.ExpenseInSource = expenseFee.ExpenseFeeInSource;
        }

        var serviceFee = await CalculateServiceFeeAsync(route);

        if (serviceFee is not null)
        {
            mappedRoute.ServiceFeeInSource = serviceFee.ServiceFeeInSource;
            mappedRoute.ServiceFeePercentage = serviceFee.ServiceFeePercentage;
        }

        return mappedRoute;
    }

    private async Task<ServiceFeeModel> CalculateServiceFeeAsync(
        Route route)
    {
        var serviceFees = await feeRepository.GetServiceFeesAsync();

        var fee = new ServiceFeeModel();

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

    private async Task<ExpenseFee?> CalculateExpenseFeeAsync(Route route)
    {
        var expenses = await feeRepository.GetExpensesAsync();


        var filterredExpenses = expenses
            .Where(x =>
                x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCLock
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCAddLockSig
                || x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCRedeem
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCRedeem);

        ExpenseFee? fee = null;

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