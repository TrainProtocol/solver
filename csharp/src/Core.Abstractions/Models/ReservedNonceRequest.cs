namespace Train.Solver.Core.Abstractions.Models;

public class ReservedNonceRequest : NextNonceRequest
{
    public string? ReferenceId { get; set; }
}
