namespace Train.Solver.Core.Errors;

public class RouteNotFoundError : NotFoundError
{
    public RouteNotFoundError() : base("Route not found")
    {
    }

    public override string ErrorCode => "ROUTE_NOT_FOUND_ERROR";
}
