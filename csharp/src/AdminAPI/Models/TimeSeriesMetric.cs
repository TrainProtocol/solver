namespace Train.Solver.AdminAPI.Models;

public class TimeSeriesMetric<T>
{
    public DateTime Date { get; set; }
    public T Value { get; set; }
}