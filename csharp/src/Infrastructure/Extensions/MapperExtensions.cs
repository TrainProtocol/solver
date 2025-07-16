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
            SourceNetwork = swap.SourceToken.Network.Name,
            SourceToken = swap.SourceToken.Asset,
            SourceAmount = BigInteger.Parse(swap.SourceAmount),
            //SourceAmountInUsd = BigInteger.Parse(swap.SourceAmount).ToUsd(swap.SourceTokenPrice, swap.SourceToken.Decimals),
            SourceAddress = swap.SourceAddress,
            DestinationNetwork = swap.DestinationToken.Network.Name,
            DestinationToken = swap.DestinationToken.Asset,
            DestinationAmount = BigInteger.Parse(swap.DestinationAmount),
            //DestinationAmountInUsd = BigInteger.Parse(swap.DestinationAmount).ToUsd(swap.DestinationTokenPrice, swap.DestinationToken.Decimals),
            DestinationAddress = swap.DestinationAddress,
            FeeAmount = BigInteger.Parse(swap.FeeAmount),
            Transactions = swap.Transactions.Select(t => t.ToDto())
        };
    }

    public static TransactionDto ToDto(this Transaction tx)
    {
        return new TransactionDto
        {
            Type = tx.Type,
            Hash = tx.TransactionHash ?? string.Empty,
            Network = tx.NetworkName
        };
    }

    public static ManagedAccountDto ToDto(this Wallet account)
    {
        return new ManagedAccountDto
        {
            Address = account.Address,
            NetworkType = account.NetworkType
        };
    }

    public static NodeDto ToDto(this Node node)
    {
        return new NodeDto
        {
            Url = node.Url,
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
            Destination = route.DestinationToken.ToWithNetworkDto(),
            MaxAmountInSource = route.MaxAmountInSource,
            Status = route.Status,
            SourceTokenGroupId = route.SourceToken.TokenGroupId,
            DestinationTokenGroupId = route.DestinationToken.TokenGroupId,
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
}
