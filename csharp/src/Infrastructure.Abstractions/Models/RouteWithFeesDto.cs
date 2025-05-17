namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteWithFeesDto : RouteDetailedDto
{
    public ExpenseFeeDto? Expenses { get; set; }

    public ServiceFeeDto? ServiceFee { get; set; }
}
