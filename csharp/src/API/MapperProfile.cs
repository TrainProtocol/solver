using AutoMapper;
using Flurl;
using Train.Solver.API.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Models;

namespace Train.Solver.API;

public class MapperProfile : Profile
{
    private const string GithubUserContentUrl = "https://raw.githubusercontent.com";

    public MapperProfile()
    {
        CreateMap<Swap, SwapDto>()
            .ForMember(
            dest => dest.CommitId,
            opt => opt.MapFrom(x => x.Id))
            .ForMember(
                dest => dest.SourceNetwork,
                opt => opt.MapFrom(x => x.SourceToken.Network.Name))
            .ForMember(
                dest => dest.SourceToken,
                opt => opt.MapFrom(x => x.SourceToken.Asset))
            .ForMember(
                dest => dest.DestinationNetwork,
                opt => opt.MapFrom(x => x.DestinationToken.Network.Name))
            .ForMember(
                dest => dest.DestinationToken,
                opt => opt.MapFrom(x => x.DestinationToken.Asset));

        CreateMap<Transaction, TransactionDto>()
           .ForMember(
             dest => dest.Type,
             opt => opt.MapFrom(x => x.Type.ToString()))
           .ForMember(
             dest => dest.Network,
             opt => opt.MapFrom(x => x.NetworkName))
           .ForMember(
             dest => dest.Hash,
             opt => opt.MapFrom(x => x.TransactionId));

        CreateMap<LimitModel, LimitDto>()
            .ForMember(
                dest => dest.MinAmountInUsd,
                opt => opt.Ignore())
            .ForMember(
                dest => dest.MaxAmountInUsd,
                opt => opt.Ignore());

        CreateMap<QuoteModel, QuoteDto>()
             .ForMember(
              dest => dest.TotalFeeInUsd,
              opt => opt.Ignore());

        CreateMap<Network, NetworkWithTokensDto>()
             .ForMember(
               dest => dest.Contracts,
               opt => opt.MapFrom(y => y.Contracts))
            .IncludeBase<Network, NetworkDto>();

        CreateMap<Network, NetworkDto>()
          .ForMember(
                 dest => dest.Logo,
                 opt => opt.MapFrom(src => GithubUserContentUrl.AppendPathSegment(src.Logo, false).ToString()))
            .ForMember(
                dest => dest.ListingDate,
                opt => opt.MapFrom(src => src.CreatedDate))
                        .ForMember(dest => dest.NativeToken, opt => opt.MapFrom(src => src.Tokens.FirstOrDefault(t => t.IsNative)));

        CreateMap<Node, NodeDto>();
        CreateMap<ManagedAccount, ManagedAccountDto>();
        CreateMap<Contract, ContractDto>();
        CreateMap<Token, TokenDto>()
            .ForMember(
                dest => dest.Symbol,
                opt => opt.MapFrom(src => src.Asset))
            .ForMember(
              dest => dest.PriceInUsd,
              opt => opt.MapFrom(x => x.TokenPrice.PriceInUsd))
            .ForMember(
                dest => dest.Contract,
                opt => opt.MapFrom(src => src.TokenContract))
              .ForMember(
                dest => dest.ListingDate,
                opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(
                 dest => dest.Logo,
                 opt => opt.MapFrom(src => GithubUserContentUrl.AppendPathSegment(src.Logo, false).ToString()));
    }
}
