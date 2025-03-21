using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Models;

public class TransactionContext
{
    public required string PrepareArgs { get; set; } = null!;

    public required TransactionType Type { get; set; }

    public string? UniquenessToken { get; set; } = null!;

    public required string NetworkName { get; set; } = null!;

    public required NetworkGroup NetworkGroup { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string SwapId { get; set; } 

    public int Attempts { get; set; }

    public Fee? Fee { get; set; }

    public string? Nonce { get; set; }

    public HashSet<string> PublishedTransactionIds { get; set; } = [];
}
