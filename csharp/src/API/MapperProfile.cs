using AutoMapper;
using Flurl;
using Train.Solver.API.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Helpers;

namespace Train.Solver.API;

public class MapperProfile : Profile
{
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
       
        CreateMap<Network, DetailedNetworkDto>()
          .ForMember(
                 dest => dest.Logo,
                 opt => opt.MapFrom(src => LogoHelpers.BuildGithubLogoUrl(src.Logo)))
            .ForMember(
                dest => dest.ListingDate,
                opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.NativeToken, opt => opt.MapFrom(src => src.NativeToken));

        CreateMap<Node, NodeDto>();
        CreateMap<ManagedAccount, ManagedAccountDto>();
        CreateMap<Contract, ContractDto>();

        CreateMap<Token, DetailedTokenDto>()
            .ForMember(
                dest => dest.Symbol,
                opt => opt.MapFrom(src => src.Asset))
            .ForMember(
                dest => dest.Contract,
                opt => opt.MapFrom(src => src.TokenContract))
              .ForMember(
                dest => dest.ListingDate,
                opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(
                 dest => dest.Logo,
                 opt => opt.MapFrom(src => LogoHelpers.BuildGithubLogoUrl(src.Logo)));
    }
}
