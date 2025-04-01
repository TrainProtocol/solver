using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Models;

public class TransactionExecutionContext
{
    public int Attempts { get; set; } = 1;

    public Fee? Fee { get; set; }

    public string? Nonce { get; set; }

    public HashSet<string> PublishedTransactionIds { get; set; } = [];
}

public class TransactionRequest : BaseRequest
{
    public required string PrepareArgs { get; set; } = null!;

    public required TransactionType Type { get; set; }

    public required NetworkType NetworkType { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string SwapId { get; set; }
}
