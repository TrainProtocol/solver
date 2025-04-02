namespace Train.Solver.Core.Abstractions.Models;

public class TransactionExecutionContext
{
    public int Attempts { get; set; } = 1;

    public Fee? Fee { get; set; }

    public string? Nonce { get; set; }

    public HashSet<string> PublishedTransactionIds { get; set; } = [];
}
