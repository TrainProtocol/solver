using FluentResults;
using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Services;

public class RouteModel
{
    public int Id { get; set; }

    public decimal MaxAmountInSource { get; set; }

    public TokenModel Source { get; set; } = null!;

    public TokenModel Destination { get; set; } = null!;

    public RouteStatus Status { get; set; }
}

public class RouteWithFeesModel : RouteModel
{
    public decimal ServiceFeeInSource { get; set; }

    public decimal ServiceFeePercentage { get; set; }

    public decimal ExpenseInSource { get; set; }
}

public class QuoteModel
{
    public RouteModel Route { get; set; } = null!;

    public decimal ReceiveAmount { get; set; }

    public decimal TotalFee { get; set; }
}

public class QuoteRequest : SourceDestinationRequest
{
    public decimal Amount { get; set; }
}

public class LimitModel
{
    public RouteModel Route { get; set; } = null!;

    public decimal MinAmount { get; set; }

    public decimal MaxAmount { get; set; }
}

public class SourceDestinationRequest
{
    public string SourceNetwork { get; set; } = null!;

    public string SourceToken { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationToken { get; set; } = null!;
}

public class FindReachableTokenRequest
{
    public string Network { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public bool FromSource { get; set; }

    public bool IncludeUnmatched { get; set; }

    public bool IncludeTempUnavailable { get; set; }
}

public class ServiceFeeModel
{
    public decimal ServiceFeePercentage { get; set; }

    public decimal ServiceFeeInSource { get; set; }
}

public class ExpenseFee
{
    public decimal ExpenseFeeInSource { get; set; }
}

public class RouteService(SolverDbContext dbContext)
{
    public const decimal MinUsdAmount = 0.69m;

    public virtual async Task<Result<IEnumerable<Token>>> GetReachablePointsAsync(
       bool fromSrcToDest,
       string? network,
       string? asset)
    {
        var requests = new List<FindReachableTokenRequest>();

        if (!string.IsNullOrEmpty(asset) && !string.IsNullOrEmpty(network))
        {
            var token = await dbContext.Tokens
                .SingleOrDefaultAsync(x =>
                    x.Asset == asset
                    && x.Network.Name == network);

            if (token is null)
            {
                return Result.Fail(new ValidationError("Invalid source network or asset"));
            }

            requests.Add(new()
            {
                Asset = asset,
                Network = network,
                FromSource = fromSrcToDest,
            });
        }
        else if (!string.IsNullOrEmpty(network) && string.IsNullOrEmpty(asset))
        {
            var tokens = await dbContext.Tokens
                .Include(x => x.Network)
                .Where(x => x.Network.Name == network)
                .ToListAsync();

            if (!tokens.Any())
            {
                return Result.Fail(new ValidationError("No tokens found for the specified network"));
            }

            requests.AddRange(tokens.Select(x => new FindReachableTokenRequest()
            {
                Asset = x.Asset,
                Network = x.Network.Name,
                FromSource = fromSrcToDest,
            }));
        }
        else if (!string.IsNullOrEmpty(asset) && string.IsNullOrEmpty(network))
        {
            return Result.Fail(new ValidationError($"{(fromSrcToDest ? "source" : "destination")}_network should be provided"));
        }

        var reachableTokens = new List<Token>();

        var routesResult = await GetActiveRoutesAsync();

        if (routesResult.IsFailed)
        {
            return routesResult.ToResult();
        }

        var routes = routesResult.Value;

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

                    var reachableRoutes = routes.Where(x => request.FromSource ? x.SourceTokenId == token.Id : x.DestinationTokenId == token.Id);

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
                return Result.Fail(new RouteNotFoundError());
            }
        }

        // In case of duplicate route edges, prioritize by status

        return Result.Ok(reachableTokens.AsEnumerable());
    }

    public virtual async Task<Result<LimitModel>> GetLimitAsync(SourceDestinationRequest request)
    {
        var routeResult = await GetActiveRouteAsync(request, amount: null);

        if (routeResult.IsFailed)
        {
            return routeResult.ToResult();
        }

        return GetLimit(routeResult.Value);
    }

    public virtual Task<Result<QuoteModel>> GetValidatedQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, GetLimit);

    public virtual Task<Result<QuoteModel>> GetQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, validatelimit: null);

    private async Task<Result<IEnumerable<Route>>> GetActiveRoutesAsync()
    {
        var routes = await GetActiveRoutesQuery(dbContext).ToListAsync();

        return routes;
    }

    private static LimitModel GetLimit(RouteWithFeesModel route)
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

    private async Task<Result<QuoteModel>> GetQuoteAsync(
        QuoteRequest request,
        Func<RouteWithFeesModel, LimitModel>? validatelimit)
    {
        var shouldValidateLimit = validatelimit is not null;

        var routeResult = await GetActiveRouteAsync(
            request,
            amount: shouldValidateLimit ? request.Amount : null);

        if (routeResult.IsFailed)
        {
            return routeResult.ToResult();
        }

        if (shouldValidateLimit)
        {
            var limit = validatelimit!(routeResult.Value);

            if (request.Amount < limit.MinAmount)
            {
                return new AmountLessThanMinError($"Amount is less than min amount {limit.MinAmount}.");
            }

            if (request.Amount > limit.MaxAmount)
            {
                return new AmountIsGreaterThanMaxError($"Amount is greater than max amount {limit.MaxAmount}.");
            }
        }

        var fixedFee = routeResult.Value.ExpenseInSource + routeResult.Value.ServiceFeeInSource;
        var percentageFee = request.Amount * routeResult.Value.ServiceFeePercentage / 100m;
        var totalFee = fixedFee + percentageFee;
        var receiveAmount = request.Amount - totalFee;

        var quote = new QuoteModel
        {
            Route = routeResult.Value,
            ReceiveAmount = receiveAmount.Truncate(routeResult.Value.Destination.Precision),
            TotalFee = totalFee
        };

        return quote;
    }

    private async Task<Result<RouteWithFeesModel>> GetActiveRouteAsync(
        SourceDestinationRequest request,
        decimal? amount)
    {
        var sourceNetworkCurrency = await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .SingleOrDefaultAsync(x =>
                x.Network.Name == request.SourceNetwork
                && x.Asset == request.SourceToken);

        var destinationNetworkCurrency = await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .SingleOrDefaultAsync(x =>
                x.Network.Name == request.DestinationNetwork
                && x.Asset == request.DestinationToken);

        if (sourceNetworkCurrency is null || destinationNetworkCurrency is null)
        {
            return new NotFoundError("Invalid source or destination");
        }

        var query = GetActiveRoutesQuery(dbContext);

        query = query.Where(x =>
            x.SourceToken.Asset == request.SourceToken
            && x.SourceToken.Network.Name == request.SourceNetwork
            && x.DestinationToken.Asset == request.DestinationToken
            && x.DestinationToken.Network.Name == request.DestinationNetwork);

        if (amount.HasValue)
        {
            query = query.Where(x => amount <= x.MaxAmountInSource);
        }

        var route = await query.FirstOrDefaultAsync();

        if (route == null)
        {
            return new RouteNotFoundError();
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
            Destination = new TokenModel
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

    private static IQueryable<Route> GetActiveRoutesQuery(SolverDbContext dbContext)
    {
        var query = dbContext.Routes
            .Include(x => x.SourceToken.Network.Nodes.Where(y => y.Type == NodeType.Public))
            .Include(x => x.SourceToken.Network.ManagedAccounts.Where(y => y.Type == AccountType.LP))
            .Include(x => x.SourceToken.Network.DeployedContracts)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes.Where(y => y.Type == NodeType.Public))
            .Include(x => x.DestinationToken.Network.ManagedAccounts.Where(y => y.Type == AccountType.LP))
            .Include(x => x.DestinationToken.Network.DeployedContracts)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => x.MaxAmountInSource > 0 && x.Status == RouteStatus.Active);

        return query;
    }

    private async Task<ServiceFeeModel> CalculateServiceFeeAsync(
        Route route)
    {
        var serviceFees = await dbContext.ServiceFees.ToListAsync();

        var fee = new ServiceFeeModel();

        if (serviceFees is not null && serviceFees.Any())
        {
            var serviceFee = MatchServiceFee(
                serviceFees,
                new()
                {
                    SourceNetwork = route.SourceToken.Network.Name,
                    SourceToken = route.SourceToken.Asset,
                    DestinationNetwork = route.DestinationToken.Network.Name,
                    DestinationToken = route.DestinationToken.Asset,
                });

            if (serviceFee != null)
            {
                fee.ServiceFeePercentage = serviceFee.FeePercentage;
                fee.ServiceFeeInSource = (serviceFee.FeeInUsd / route.SourceToken.TokenPrice.PriceInUsd).Truncate(route.SourceToken.Precision);
            }
        }

        return fee;
    }

    private static ServiceFee? MatchServiceFee(
        IEnumerable<ServiceFee> feeSettings,
        SourceDestinationRequest request)
    {
        if (feeSettings.Any())
        {
            // Helper methods to check matches and nullity
            bool matches(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

            // Consolidated match checkers
            bool matchesSource(ServiceFee x) => matches(x.SourceNetwork, request.SourceNetwork) && matches(x.SourceAsset, request.SourceToken);
            bool matchesDestination(ServiceFee x) => matches(x.DestinationNetwork, request.DestinationNetwork) && matches(x.DestinationAsset, request.DestinationToken);
            bool isSourceNull(ServiceFee x) => x.SourceNetwork is null && x.SourceAsset is null;
            bool isDestinationNull(ServiceFee x) => x.DestinationNetwork is null && x.DestinationAsset is null;

            // Specific scenario matchers using the helper and consolidated methods
            bool matchExactSourceDestNetworkAssets(ServiceFee x) => matchesSource(x) && matchesDestination(x);
            bool matchSourceAssetNullOthers(ServiceFee x) => matchesSource(x) && isDestinationNull(x);
            bool matchDestAssetNullOthers(ServiceFee x) => isSourceNull(x) && matchesDestination(x);
            bool matchSourceNetworkNullOthers(ServiceFee x) => matches(x.SourceNetwork, request.SourceNetwork) && isDestinationNull(x) && x.SourceAsset is null;
            bool matchDestNetworkNullOthers(ServiceFee x) => isSourceNull(x) && matches(x.DestinationNetwork, request.DestinationNetwork) && x.DestinationAsset is null;
            bool matchGlobalFee(ServiceFee x) => isSourceNull(x) && isDestinationNull(x);

            var matchers = new Func<ServiceFee, bool>[] {
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
        var expenses = await dbContext.Expenses
              .Include(x => x.FeeToken.TokenPrice)
              .ToListAsync();


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
                var expenseFeeAmountInUsd = transactionCompletionDetail.FeeAmount * transactionCompletionDetail.FeeToken.TokenPrice.PriceInUsd;
                fee.ExpenseFeeInSource += (expenseFeeAmountInUsd / route.SourceToken.TokenPrice.PriceInUsd).Truncate(route.SourceToken.Precision);
            }
        }

        return fee;
    }
}
