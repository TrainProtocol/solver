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

    public void AddFeeValue(decimal value)
    {
        if (LastFeeValues.Length == 0)
        {
            LastFeeValues = LastFeeValues.Append(value).ToArray();
        }
        else
        {
            if (value > LastFeeValues.Average() * 30)
            {
                return;
            }

            LastFeeValues = LastFeeValues.Append(value).ToArray();
            if (LastFeeValues.Length > 10)
            {
                LastFeeValues = LastFeeValues.Skip(LastFeeValues.Length - 10).ToArray();
            }
        }
    }
}