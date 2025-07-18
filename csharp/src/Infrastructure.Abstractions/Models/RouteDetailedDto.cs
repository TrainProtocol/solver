using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDetailedDto : RouteDto
{
    public int Id { get; set; }
   
    public string MaxAmountInSource { get; set; } = null!;

    public RouteStatus Status { get; set; }

    public string RateProviderName { get; set; }
}
