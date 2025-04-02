using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public class Expense : EntityBase<int>
{
    public decimal FeeAmount => LastFeeValues.Length > 0 ? LastFeeValues.Average() : default;

    public int FeeTokenId { get; set; }

    public int TokenId { get; set; }

    public decimal[] LastFeeValues { get; set; } = [];

    public TransactionType TransactionType { get; set; }

    public Token Token { get; set; } = null!;

    public Token FeeToken { get; set; } = null!;    
}