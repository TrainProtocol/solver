using Microsoft.Extensions.Options;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Util.Extensions;
using Train.Solver.Util.Helpers;
using Nethereum.Util;
using Train.Solver.Util;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using System.Numerics;

namespace Train.Solver.Infrastructure.MarketMaker;

public class RouteService(
    IRouteRepository routeRepository,
    IWalletRepository walletRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IRateService rateService,
    IOptions<TrainSolverOptions> options) : IRouteService
{
    public const decimal MinUsdAmount = 0.69m;

    //public async Task<IEnumerable<DetailedNetworkDto>?> GetSourcesAsync(string? networkName, string? token)
    //{
    //    return await GetReachablePointsAsync(
    //        fromSrcToDest: false,
    //        networkName: networkName,
    //        asset: token);
    //}

    //public async Task<IEnumerable<DetailedNetworkDto>?> GetDestinationsAsync(string? networkName, string? token)
    //{
    //    return await GetReachablePointsAsync(
    //       fromSrcToDest: true,
    //       networkName: networkName,
    //       asset: token);
    //}

    //private async Task<IEnumerable<DetailedNetworkDto>?> GetReachablePointsAsync(
    //    bool fromSrcToDest,
    //    string? networkName,
    //    string? asset)
    //{
    //    if (!string.IsNullOrEmpty(asset) && string.IsNullOrEmpty(networkName)
    //        || !string.IsNullOrEmpty(networkName) && string.IsNullOrEmpty(asset))
    //    {
    //        throw new Exception($"{(fromSrcToDest ? "Source" : "Destination")} network and token should be provided");
    //    }

    //    Token? reuqestedPoint = null;

    //    if (!string.IsNullOrEmpty(asset) && !string.IsNullOrEmpty(networkName))
    //    {
    //        reuqestedPoint = await networkRepository.GetTokenAsync(networkName, asset);

    //        if (reuqestedPoint == null)
    //        {
    //            return null;
    //        }
    //    }

    //    var reachablePoints = await routeRepository.GetReachablePointsAsync(
    //        [RouteStatus.Active],
    //        fromSrcToDest,
    //        reuqestedPoint?.Id);

    //    if (reachablePoints == null || !reachablePoints.Any())
    //    {
    //        return null;
    //    }

    //    var tokens = await networkRepository.GetTokensAsync(reachablePoints.ToArray());

    //    var networksWithAmounts = tokens
    //        .GroupBy(x => x.Network.Name)
    //        .Select(x =>
    //        {
    //            var network = x.First().Network;
    //            var networkWithTokens = new DetailedNetworkDto
    //            {
    //                Name = x.Key,
    //                Type = network.Type,
    //                ChainId = network.ChainId,
    //                DisplayName = network.DisplayName,
    //                HTLCNativeContractAddress = network.HTLCNativeContractAddress,
    //                HTLCTokenContractAddress = network.HTLCTokenContractAddress,
    //                Tokens = x.Select(x => x.ToDto()),
    //                Nodes = network.Nodes.Select(x => x.ToDto()),
    //                NativeToken = network.NativeToken?.ToDto(),
    //            };

    //            return networkWithTokens;

    //        });

    //    return networksWithAmounts;
    //}

    public virtual async Task<LimitDto> GetLimitAsync(SourceDestinationRequest request)
    {
        var route = await routeRepository.GetAsync(
            request.SourceNetwork,
            request.SourceToken,
            request.DestinationNetwork,
            request.DestinationToken,
            null);

        if (route is null)
        {
            throw new RouteNotFoundException($"Route not found.");
        }

        return await GetLimitAsync(route);
    }

    public virtual Task<QuoteWithSolverDto> GetValidatedQuoteAsync(
        QuoteRequest request) => GetQuoteAsync(request, GetLimitAsync);

    public virtual Task<QuoteWithSolverDto> GetQuoteAsync(
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
            MinAmount = minAmount,
            //MinAmountInUsd = (TokenUnitConverter.FromBaseUnits(minAmount, route.SourceToken.Decimals) * route.SourceToken.TokenPrice.PriceInUsd).Truncate(2),
            MaxAmount = TokenUnitConverter.ToBaseUnits(route.MaxAmountInSource, route.SourceToken.Decimals),
            //MaxAmountInUsd = (route.MaxAmountInSource * route.SourceToken.TokenPrice.PriceInUsd).Truncate(2),
        };
    }

    private async Task<QuoteWithSolverDto> GetQuoteAsync(
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
            throw new RouteNotFoundException($"Route not found.");
        }

        if (shouldValidateLimit)
        {
            var limit = await validatelimit!(route);

            if (request.Amount < limit.MinAmount)
            {
                throw new InvalidAmountException($"Amount is less than min amount {limit.MinAmount}.");
            }

            if (request.Amount > limit.MaxAmount)
            {
                throw new InvalidAmountException($"Amount is greater than max amount {limit.MaxAmount}.");
            }
        }

        var swapRate = await rateService.GetRateAsync(route);
        var amount = request.Amount;
        var totalFee = await CalculateTotalFeeAsync(route, amount);
        var actualAmountToSwap = amount - totalFee;
        var receiveAmount = actualAmountToSwap.ConvertTokenAmount(swapRate, route.SourceToken.Decimals, route.DestinationToken.Decimals);

        var wallet = await walletRepository.GetDefaultAsync(route.SourceToken.Network.Type);

        if (wallet == null)
        {
            throw new Exception($"Solver account not found for network {route.SourceToken.Network.Name}");
        }

        var quote = new QuoteWithSolverDto
        {
            //SourceAmount = request.Amount,
            //SourceAmountInUsd = request.Amount.ToUsd(route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals).Truncate(2),
            ReceiveAmount = receiveAmount,
            //ReceiveAmountInUsd = receiveAmount.ToUsd(route.DestinationToken.TokenPrice.PriceInUsd, route.DestinationToken.Decimals),
            TotalFee = totalFee,
            //TotalFeeInUsd = totalFee.ToUsd(route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals),
            SolverAddress = wallet.Address,
            ContractAddress =
                route.SourceToken.Id == route.SourceToken.Network.NativeTokenId
                ? route.SourceToken.Network.HTLCNativeContractAddress
                : route.SourceToken.Network.HTLCTokenContractAddress,
        };

        return quote;
    }

    private async Task<BigInteger> CalculateTotalFeeAsync(Route route, BigInteger amount)
    {
        BigInteger fixedFee = default;
        BigInteger percentageFee = default;

        var expenseFee = await CalculateExpenseFeeAsync(route);

        if (expenseFee is not null && !options.Value.DisableExpenseFee)
        {
            fixedFee += expenseFee.ExpenseFee;
        }

        var serviceFee = CalculateServiceFee(route);

        if (serviceFee is not null)
        {
            fixedFee += serviceFee.ServiceFee;
            percentageFee = amount.PercentOf(serviceFee.ServiceFeePercentage);
        }

        var totalFee = fixedFee + percentageFee;

        return totalFee;
    }

    private ServiceFeeDto CalculateServiceFee(
        Route route)
    {
        var fee = new ServiceFeeDto()
        {
            ServiceFee = BigInteger.Zero,
            ServiceFeePercentage = default
        };

        if (route.ServiceFee != null)
        {
            fee.ServiceFeePercentage = route.ServiceFee.FeePercentage;
            fee.ServiceFee = TokenUnitConverter.ToBaseUnits(
                route.ServiceFee.FeeInUsd / route.SourceToken.TokenPrice.PriceInUsd,
                route.SourceToken.Decimals);
        }

        return fee;
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

                fee.ExpenseFee += TokenUnitConverter.ToBaseUnits(expenseFeeAmountInUsd / route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals);
            }
        }

        return fee;
    }
}
