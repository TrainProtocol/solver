using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public class Swap : EntityBase<string>
{
    public int SourceTokenId { get; set; }

    public Token SourceToken { get; set; } = null!;

    public int DestinationTokenId { get; set; }

    public Token DestinationToken { get; set; } = null!;

    public string SourceAddress { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public decimal SourceAmount { get; set; }

    public string Hashlock { get; set; } = null!;

    public decimal DestinationAmount { get; set; }

    public decimal FeeAmount { get; set; }

    public virtual List<Transaction> Transactions { get; set; } = [];
}
