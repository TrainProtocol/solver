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
    IFeeRepository feeRepository,
    IRateService rateService,
    IOptions<TrainSolverOptions> options) : IRouteService
{
    public const decimal MinUsdAmount = 0.69m;

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
            MaxAmount = TokenUnitConverter.ToBaseUnits(route.MaxAmountInSource, route.SourceToken.Decimals),
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

        var quote = new QuoteWithSolverDto
        {
            ReceiveAmount = receiveAmount,
            TotalFee = totalFee,
            SolverAddress = route.DestinationWallet.Address,
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
