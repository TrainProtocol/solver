using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Helpers;

namespace Train.Solver.Infrastructure.Extensions;

public static class MapperExtensions
{
    public static SwapDto ToDto(this Swap swap)
    {
        return new SwapDto
        {
            CommitId = swap.Id,
            SourceNetwork = swap.SourceToken.Network.Name,
            SourceToken = swap.SourceToken.Asset,
            SourceAmount = swap.SourceAmount,
            SourceAddress = swap.SourceAddress,
            DestinationNetwork = swap.DestinationToken.Network.Name,
            DestinationToken = swap.DestinationToken.Asset,
            DestinationAmount = swap.DestinationAmount,
            DestinationAddress = swap.DestinationAddress,
            FeeAmount = swap.FeeAmount,
            Transactions = swap.Transactions.Select(t => t.ToDto())
        };
    }

    public static TransactionDto ToDto(this Transaction tx)
    {
        return new TransactionDto
        {
            Type = tx.Type,
            Hash = tx.TransactionId ?? string.Empty,
            Network = tx.NetworkName
        };
    }

    public static ContractDto ToDto(this Contract contract)
    {
        return new ContractDto
        {
            Type = contract.Type,
            Address = contract.Address
        };
    }

    public static ManagedAccountDto ToDto(this ManagedAccount account)
    {
        return new ManagedAccountDto
        {
            Address = account.Address,
            Type = account.Type
        };
    }

    public static NodeDto ToDto(this Node node)
    {
        return new NodeDto
        {
            Url = node.Url,
            Type = node.Type
        };
    }

    public static NetworkDto ToDto(this Network network)
    {
        return new NetworkDto
        {
            Name = network.Name,
            DisplayName = network.DisplayName,
            ChainId = network.ChainId,
            FeeType = network.FeeType,
            Type = network.Type,
            IsTestnet = network.IsTestnet
        };
    }

    public static TokenDto ToDto(this Token token)
    {
        return new TokenDto
        {
            Symbol = token.Asset,
            Contract = token.TokenContract,
            Decimals = token.Decimals,
            Precision = token.Precision,
            PriceInUsd = token.TokenPrice.PriceInUsd
        };
    }

    public static DetailedTokenDto ToDetailedDto(this Token token)
    {
        var dto = new DetailedTokenDto();
        MapBaseTokenFields(token, dto);
        dto.Logo = LogoHelpers.BuildGithubLogoUrl(token.Logo);
        dto.ListingDate = token.CreatedDate; 
        return dto;
    }

    public static TokenWithNetworkDto ToWithNetworkDto(this Token token)
    {
        var dto = new TokenWithNetworkDto();
        MapBaseTokenFields(token, dto);
        dto.Network = token.Network.ToDto();
        return dto;
    }

    public static DetailedNetworkDto ToDetailedDto(this Network network)
    {
        var dto = new DetailedNetworkDto
        {
            Name = network.Name,
            DisplayName = network.DisplayName,
            ChainId = network.ChainId,
            FeeType = network.FeeType,
            Type = network.Type,
            IsTestnet = network.IsTestnet,
            Logo = network.Logo,
            TransactionExplorerTemplate = network.TransactionExplorerTemplate,
            AccountExplorerTemplate = network.AccountExplorerTemplate,
            ListingDate = network.CreatedDate, 
            NativeToken = network.NativeToken?.ToDetailedDto(),
            Tokens = network.Tokens.Select(t => t.ToDetailedDto()),
            Nodes = network.Nodes.Select(n => n.ToDto()),
            Contracts = network.Contracts.Select(c => c.ToDto()),
            ManagedAccounts = network.ManagedAccounts.Select(a => a.ToDto())
        };

        return dto;
    }

    public static RouteDto ToDto(this Route route)
    {
        return new RouteDto
        {
            Id = route.Id,
            Source = route.SourceToken.ToWithNetworkDto(),
            Destionation = route.DestinationToken.ToWithNetworkDto(),
            MaxAmountInSource = route.MaxAmountInSource,
            Status = route.Status
        };
    }

    public static RouteWithFeesDto ToWithFeesDto(this Route route)
    {
        return new RouteWithFeesDto
        {
            Id = route.Id,
            Source = route.SourceToken.ToWithNetworkDto(),
            Destionation = route.DestinationToken.ToWithNetworkDto(),
            MaxAmountInSource = route.MaxAmountInSource,
            Status = route.Status
        };
    }


    private static void MapBaseTokenFields(Token token, TokenDto dto)
    {
        dto.Symbol = token.Asset;
        dto.Contract = token.TokenContract;
        dto.Decimals = token.Decimals;
        dto.Precision = token.Precision;
        dto.PriceInUsd = token.TokenPrice.PriceInUsd;
    }

}
