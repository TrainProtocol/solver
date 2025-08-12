using Microsoft.Extensions.Options;
using System;
using System.Numerics;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Common.Helpers;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.Infrastructure.Services;

public class QuoteService(
    IRouteRepository routeRepository,
    IFeeRepository feeRepository,
    KeyedServiceResolver<IRateProvider> rateProviderResolver,
    IOptions<TrainSolverOptions> options) : IQuoteService
{
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
        var minBufferAmount = BigInteger.Parse(route.MinAmountInSource);

        var (TotalFee, _, __) = await CalculateTotalFeeAsync(route, minBufferAmount);
        var minAmount = minBufferAmount + TotalFee;
        var maxAmount = BigInteger.Parse(route.MaxAmountInSource);

        return new LimitDto
        {
            MinAmount = minAmount,
            MaxAmount = maxAmount,
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

        var rateProvider = rateProviderResolver.Resolve(route.RateProvider.Name);
        var swapRate = await rateProvider.GetRateAsync(route.ToDto());
        var amount = request.Amount;
        var (TotalFee, TotalServiceFee, TotalExpenseFee) = await CalculateTotalFeeAsync(route, amount);
        var actualAmountToSwap = amount - TotalFee;
        var receiveAmount = actualAmountToSwap.ConvertTokenAmount(            
            route.SourceToken.Decimals,
            route.DestinationToken.Decimals,
            swapRate);

        var quote = new QuoteWithSolverDto
        {
            ReceiveAmount = receiveAmount,
            TotalFee = TotalFee,
            TotalServiceFee = TotalServiceFee,
            TotalExpenseFee = TotalExpenseFee,
            SourceSolverAddress = route.SourceWallet.Address,
            SourceSignerAgent = route.SourceWallet.SignerAgent.Name,
            DestinationSolverAddress = route.DestinationWallet.Address,
            DestinationSignerAgent = route.DestinationWallet.SignerAgent.Name,
            SourceContractAddress =
                route.SourceToken.Id == route.SourceToken.Network.NativeTokenId
                ? route.SourceToken.Network.HTLCNativeContractAddress
                : route.SourceToken.Network.HTLCTokenContractAddress,
            DestinationContractAddress =
                route.DestinationToken.Id == route.DestinationToken.Network.NativeTokenId
                ? route.DestinationToken.Network.HTLCNativeContractAddress
                : route.DestinationToken.Network.HTLCTokenContractAddress,
        };

        return quote;
    }

    private async Task<(BigInteger TotalFee, BigInteger TotalServiceFee, BigInteger TotalExpenseFee)> CalculateTotalFeeAsync(Route route, BigInteger amount)
    {
        BigInteger fixedFee = default;
        BigInteger? expenseFee = default;
        BigInteger percentageFee = default;


        if (!route.IgnoreExpenseFee)
        {
            expenseFee = await CalculateExpenseFeeAsync(route);

            if (expenseFee != null)
            {
                fixedFee += expenseFee.Value;
            }
        }

        var (Fee, Percentage) = CalculateServiceFee(route);

        fixedFee += Fee;
        percentageFee = amount.PercentOf(Percentage);

        var totalFee = fixedFee + percentageFee;
        var totalServiceFee = Fee + percentageFee;

        return (totalFee, totalServiceFee, expenseFee.GetValueOrDefault());
    }

    private static (BigInteger Fee, decimal Percentage) CalculateServiceFee(
        Route route)
    {
        var fee = BigInteger.Zero;
        var percentage = default(decimal);

        if (route.ServiceFee != null)
        {
            percentage = route.ServiceFee.FeePercentage;
            fee = TokenUnitHelper.ToBaseUnits(
                route.ServiceFee.FeeInUsd / route.SourceToken.TokenPrice.PriceInUsd,
                route.SourceToken.Decimals);
        }

        return (fee, percentage);
    }

    private async Task<BigInteger?> CalculateExpenseFeeAsync(Route route)
    {
        var expenses = await feeRepository.GetExpensesAsync();

        var filterredExpenses = expenses
            .Where(x =>
                x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCLock
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCAddLockSig
                || x.TokenId == route.DestinationTokenId && x.TransactionType == TransactionType.HTLCRedeem
                || x.TokenId == route.SourceTokenId && x.TransactionType == TransactionType.HTLCRedeem);

        BigInteger? fee = null;

        if (filterredExpenses.Any())
        {
            fee = new();

            foreach (var transactionCompletionDetail in filterredExpenses)
            {
                var expenseFeeAmountInUsd = BigInteger.Parse(transactionCompletionDetail.FeeAmount).ToUsd(
                    transactionCompletionDetail.FeeToken.TokenPrice.PriceInUsd,
                    transactionCompletionDetail.FeeToken.Decimals);

                fee += TokenUnitHelper.ToBaseUnits(expenseFeeAmountInUsd / route.SourceToken.TokenPrice.PriceInUsd, route.SourceToken.Decimals);
            }
        }

        return fee;
    }
}
