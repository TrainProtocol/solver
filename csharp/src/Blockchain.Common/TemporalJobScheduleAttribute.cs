namespace Train.Solver.Blockchain.Common;

public class TemporalJobScheduleAttribute : Attribute
{
    public required string Chron { get; set; }
}
