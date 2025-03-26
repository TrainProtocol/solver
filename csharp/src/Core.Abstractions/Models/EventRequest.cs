namespace Train.Solver.Core.Abstractions.Models;
public class EventRequest : BaseRequest
{
    public required ulong FromBlock { get; set; }

    public required ulong ToBlock { get; set;}
}
