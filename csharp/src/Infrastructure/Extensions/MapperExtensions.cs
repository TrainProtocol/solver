using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Extensions;

public static class MapperExtensions
{
    public static SwapDto ToDto(this Swap swap)
    {
        return new SwapDto
        {
            CommitId = swap.CommitId,
            Hashlock = swap.Hashlock,
            Source = swap.Route.SourceToken.ToWithNetworkDto(),
            SourceAmount = BigInteger.Parse(swap.SourceAmount),
            SourceAddress = swap.SourceAddress,
            SourceContractAddress = swap.Route.SourceTokenId == swap.Route.SourceToken.Network.NativeTokenId ? 
                swap.Route.SourceToken.Network.HTLCNativeContractAddress :
                swap.Route.SourceToken.Network.HTLCTokenContractAddress,
            Destination = swap.Route.DestinationToken.ToWithNetworkDto(),
            DestinationAddress = swap.DestinationAddress,
            DestinationContractAddress = swap.Route.DestinationTokenId == swap.Route.DestinationToken.Network.NativeTokenId ?
                swap.Route.DestinationToken.Network.HTLCNativeContractAddress :
                swap.Route.DestinationToken.Network.HTLCTokenContractAddress,
            FeeAmount = BigInteger.Parse(swap.FeeAmount),
            Transactions = swap.Transactions.Select(t => t.ToDto()),
            DestinationAmount = BigInteger.Parse(swap.DestinationAmount),
        };
    }

    public static TransactionDto ToDto(this Transaction tx)
    {
        return new TransactionDto
        {
            Type = tx.Type,
            Hash = tx.TransactionHash ?? string.Empty,
            Network = tx.Network.Name
        };
    }

    public static NodeDto ToDto(this Node node)
    {
        return new NodeDto
        {
            Url = node.Url,
            ProviderName = node.ProviderName,
        };
    }

    public static NetworkDto ToDto(this Network network)
    {
        return new NetworkDto
        {
            Name = network.Name,
            ChainId = network.ChainId,
            Type = network.Type,
        };
    }

    public static TokenDto ToDto(this Token token)
    {
        return new TokenDto
        {
            Symbol = token.Asset,
            Contract = token.TokenContract,
            Decimals = token.Decimals,
        };
    }

    public static TokenNetworkDto ToWithNetworkDto(this Token token)
    {
        var dto = new TokenNetworkDto
        {
            Network = token.Network.ToDto(),
            Token = token.ToDto()
        };

        return dto;
    }

    public static DetailedNetworkDto ToDetailedDto(this Network network)
    {
        var dto = new DetailedNetworkDto
        {
            Name = network.Name,
            DisplayName = network.DisplayName,
            ChainId = network.ChainId,
            Type = network.Type,
            FeeType = network.FeeType,
            FeePercentageIncrease = network.FeePercentageIncrease,
            HTLCNativeContractAddress = network.HTLCNativeContractAddress,
            HTLCTokenContractAddress = network.HTLCTokenContractAddress,
            NativeToken = network.NativeToken?.ToDto(),
            Tokens = network.Tokens.Select(t => t.ToDto()),
            Nodes = network.Nodes.Select(n => n.ToDto()),
        };

        return dto;
    }

    public static RouteDetailedDto ToDetailedDto(this Route route)
    {
        return new RouteDetailedDto
        {
            Id = route.Id,
            Source = route.SourceToken.ToWithNetworkDto(),
            SourceWallet = route.SourceWallet.Address,
            Destination = route.DestinationToken.ToWithNetworkDto(),
            DestinationWallet = route.DestinationWallet.Address,
            MaxAmountInSource = route.MaxAmountInSource,
            Status = route.Status,
            RateProviderName = route.RateProvider.Name,
        };
    }

    public static RouteDto ToDto(this Route route)
    {
        return new RouteDto
        {
            Source = route.SourceToken.ToWithNetworkDto(),
            Destination = route.DestinationToken.ToWithNetworkDto(),
        };
    }

    public static WalletDto ToDto(this Wallet wallet)
    {
        return new WalletDto
        {
            Name = wallet.Name,
            Address = wallet.Address,
            NetworkType = wallet.NetworkType
        };
    }

    public static TrustedWalletDto ToDto(this TrustedWallet wallet)
    {
        return new TrustedWalletDto
        {
            Name = wallet.Name,
            Address = wallet.Address,
            NetworkType = wallet.NetworkType
        };
    }

    public static ServiceFeeDto ToDto(this ServiceFee serviceFee)
    {
        return new ServiceFeeDto
        {
           Name = serviceFee.Name,
            Percentage = serviceFee.FeePercentage,
           UsdAmount = serviceFee.FeeInUsd,
        };
    }

    public static RateProviderDto ToDto(this RateProvider rateProvider)
    {
        return new RateProviderDto
        {
            Name = rateProvider.Name,
        };
    }
}
