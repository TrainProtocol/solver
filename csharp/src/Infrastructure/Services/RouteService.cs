using Train.Solver.API.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Extensions;
using Train.Solver.Util.Helpers;

namespace Train.Solver.Infrastructure.Services;

public class RouteService(
    IRouteRepository routeRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository) : IRouteService
{
    public const decimal MinUsdAmount = 0.69m;

    public async Task<IEnumerable<NetworkWithTokensDto>?> GetSourcesAsync(string? networkName, string? token)
    {
        return await GetReachablePointsAsync(
            fromSrcToDest: true,
            networkName: networkName,
            asset: token);
    }

    public async Task<IEnumerable<NetworkWithTokensDto>?> GetDestinationsAsync(string? networkName, string? token)
    {
        return await GetReachablePointsAsync(
           fromSrcToDest: true,
           networkName: networkName,
           asset: token);
    }

    private async Task<IEnumerable<NetworkWithTokensDto>?> GetReachablePointsAsync(
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
                var networkWithTokens = new NetworkWithTokensDto
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
                    Contracts = network.Contracts.Select(c => new ContractDto
                    {
                        Address = c.Address,
                        Type = c.Type,
                    }),
                    Tokens = x.Select(t => new TokenDto
                    {
                        ListingDate = t.CreatedDate,
                        Contract = t.TokenContract,
                        Decimals = t.Decimals,
                        Symbol = t.Asset,
                        Precision = t.Precision,
                        PriceInUsd = t.TokenPrice.PriceInUsd,
                        Logo = LogoHelpers.BuildGithubLogoUrl(t.Logo),
                    }),
                    ManagedAccounts = network.ManagedAccounts.Select(m => new ManagedAccountDto
                    {
                        Address = m.Address,
                        Type = m.Type,
                    }),
                    Nodes = network.Nodes.Where(x => x.Type == NodeType.Public).Select(n => new NodeDto
                    {
                        Url = n.Url,
                        Type = n.Type,
                    })
                };

                if (network.NativeToken != null)
                {
                    networkWithTokens.NativeToken = new TokenDto
                    {
                        ListingDate = network.NativeToken.CreatedDate,
                        Contract = network.NativeToken.TokenContract,
                        Decimals = network.NativeToken.Decimals,
                        Symbol = network.NativeToken.Asset,
                        Precision = network.NativeToken.Precision,
                        PriceInUsd = network.NativeToken.TokenPrice.PriceInUsd,
                        Logo = LogoHelpers.BuildGithubLogoUrl(network.NativeToken.Logo),
                    };
                }

                return networkWithTokens;

            });
        // Build

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

    private static LimitDto GetLimit(RouteWithFeesModel route)
    {
        var minBufferAmount = MinUsdAmount / route.Source.UsdPrice;

        var fixedFee = route.ExpenseInSource + route.ServiceFeeInSource;
        var percentageFee = (minBufferAmount + fixedFee) * route.ServiceFeePercentage / 100m;
        var totalFee = fixedFee + percentageFee;
        var minAmount = minBufferAmount + totalFee;

        return new LimitDto
        {
            MinAmount = minAmount.Truncate(route.Source.Precision),
            MinAmountInUsd = (minAmount * route.Source.UsdPrice).Truncate(2),
            MaxAmount = route.MaxAmountInSource.Truncate(route.Source.Precision),
            MaxAmountInUsd = (route.MaxAmountInSource * route.Source.UsdPrice).Truncate(2),
        };
    }

    private async Task<QuoteDto?> GetQuoteAsync(
        QuoteRequest request,
        Func<RouteWithFeesModel, LimitDto>? validatelimit)
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

        var fixedFee = route.ExpenseInSource + route.ServiceFeeInSource;
        var percentageFee = request.Amount * route.ServiceFeePercentage / 100m;
        var totalFee = fixedFee + percentageFee;
        var receiveAmount = request.Amount - totalFee;

        var quote = new QuoteDto
        {
            ReceiveAmount = receiveAmount.Truncate(route.Destionation.Precision),
            TotalFee = totalFee,
            TotalFeeInUsd = totalFee * route.Source.UsdPrice,
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

        if (route is null)
        {
            return null;
        }

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