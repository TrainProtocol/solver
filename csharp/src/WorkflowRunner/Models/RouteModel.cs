using Train.Solver.Data.Entities;

namespace Train.Solver.WorkflowRunner.Models;

public class RouteModel
{
    public int Id { get; set; }

    public TokenModel SourceTokenModel { get; set; } = null!;

    public TokenModel DestionationTokenModel { get; set; } = null!;

    public decimal MaxAmountInSource { get; set; }

    public RouteStatus Status { get; set; }

    public override bool Equals(object? obj)
    {
        var typedObj = obj as RouteModel;

        return Id == typedObj.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

}
