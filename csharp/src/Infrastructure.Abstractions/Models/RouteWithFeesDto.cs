namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteWithFeesDto : RouteDto
{
    public ExpenseFeeDto? Expenses { get; set; }

    public ServiceFeeDto? ServiceFee { get; set; }
}
