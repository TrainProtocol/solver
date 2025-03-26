namespace Train.Solver.Core.Models;

public class ReservedNonceRequest : NextNonceRequest
{
    public string? ReferenceId { get; set; }
}
