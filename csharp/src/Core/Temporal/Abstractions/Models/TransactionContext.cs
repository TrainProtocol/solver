using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Temporal.Abstractions.Models;

public class TransactionContext
{
    public string PrepareArgs { get; set; } = null!;

    public TransactionType Type { get; set; }

    public string CorrelationId { get; set; } = null!;

    public string UniquenessToken { get; set; } = null!;

    public string NetworkName { get; set; } = null!;

    public string FromAddress { get; set; } = null!;

    public bool SubtractFeeFromAmount { get; set; }

    public Fee? Fee { get; set; }

    public int Attempts { get; set; }

    public string? Nonce { get; set; }

    public HashSet<string> PublishedTransactionIds { get; set; } = [];
}
