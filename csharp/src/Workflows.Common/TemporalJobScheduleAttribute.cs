namespace Train.Solver.Workflows.Common;

public class TemporalJobScheduleAttribute : Attribute
{
    public required string Chron { get; set; }
}
