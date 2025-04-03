using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

public class TransactionRequest : BaseRequest
{
    public required string PrepareArgs { get; set; } = null!;

    public required TransactionType Type { get; set; }

    public required NetworkType NetworkType { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string SwapId { get; set; }
}
