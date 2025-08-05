namespace Train.Solver.Workflow.Common;

public class TemporalJobScheduleAttribute : Attribute
{
    public required string Chron { get; set; }
}
