namespace Train.Solver.Blockchain.Abstractions.Models;
public class EventRequest : BaseRequest
{
    public required ulong FromBlock { get; set; }

    public required ulong ToBlock { get; set;}
}
