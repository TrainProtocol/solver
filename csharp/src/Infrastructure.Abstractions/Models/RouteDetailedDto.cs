using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDetailedDto : RouteDto
{
    public int Id { get; set; }
   
    public decimal MaxAmountInSource { get; set; }

    public RouteStatus Status { get; set; }
}
