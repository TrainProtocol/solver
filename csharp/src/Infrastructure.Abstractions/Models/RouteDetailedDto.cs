using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDetailedDto : RouteDto
{
    public required int Id { get; set; }

    public required string MinAmountInSource { get; set; }

    public required string MaxAmountInSource { get; set; } 

    public required RouteStatus Status { get; set; }

    public required string RateProviderName { get; set; } 

    public required string SourceWallet { get; set; } 

    public required string DestinationWallet { get; set; } 
}
