using AutoMapper;
using Flurl;
using Train.Solver.API.Models;
using Train.Solver.Data.Entities;

namespace Train.Solver.API;

public class MapperProfile : Profile
{
    private const string GithubUserContentUrl = "https://raw.githubusercontent.com";

    public MapperProfile()
    {
        CreateMap<Swap, SwapModel>()
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

        CreateMap<Transaction, TransactionModel>()
           .ForMember(
             dest => dest.Type,
             opt => opt.MapFrom(x => x.Type.ToString()))
           .ForMember(
             dest => dest.Network,
             opt => opt.MapFrom(x => x.NetworkName))
           .ForMember(
             dest => dest.Hash,
             opt => opt.MapFrom(x => x.TransactionId));

        CreateMap<Core.Services.LimitModel, API.Models.LimitModel>()
            .ForMember(
                dest => dest.MinAmountInUsd,
                opt => opt.Ignore())
            .ForMember(
                dest => dest.MaxAmountInUsd,
                opt => opt.Ignore());

        CreateMap<Core.Services.QuoteModel, API.Models.QuoteModel>()
             .ForMember(
              dest => dest.TotalFeeInUsd,
              opt => opt.Ignore());

        CreateMap<Network, NetworkWithTokensModel>()
             .ForMember(
               dest => dest.Contracts,
               opt => opt.MapFrom(y => y.DeployedContracts))
            .IncludeBase<Network, NetworkModel>();

        CreateMap<Network, NetworkModel>()
          .ForMember(
                 dest => dest.Logo,
                 opt => opt.MapFrom(src => GithubUserContentUrl.AppendPathSegment(src.Logo, false).ToString()))
            .ForMember(
                dest => dest.ListingDate,
                opt => opt.MapFrom(src => src.CreatedDate))
                        .ForMember(dest => dest.NativeToken, opt => opt.MapFrom(src => src.Tokens.FirstOrDefault(t => t.IsNative)));

        CreateMap<Node, NodeModel>();
        CreateMap<ManagedAccount, ManagedAccountModel>();
        CreateMap<Contract, ContractModel>();
        CreateMap<Token, TokenModel>()
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
