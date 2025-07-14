using System.Numerics;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class Swap : EntityBase
{
    public string CommitId { get; set; } = null!;

    public int SourceTokenId { get; set; }

    public Token SourceToken { get; set; } = null!;

    public int DestinationTokenId { get; set; }

    public Token DestinationToken { get; set; } = null!;

    //public decimal SourceTokenPrice { get; set; }

    //public decimal DestinationTokenPrice { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public string SourceAmount { get; set; } = null!;

    public string DestinationAmount { get; set; } = null!;

    public string FeeAmount { get; set; } = null!;

    public virtual List<Transaction> Transactions { get; set; } = [];
}
