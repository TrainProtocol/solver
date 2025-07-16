using System.Numerics;
using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Util.Enums;
using Train.Solver.Util.Extensions;

namespace Train.Solver.Data.Abstractions.Entities;

public class Expense : EntityBase
{
    public string FeeAmount => LastFeeValues.Length > 0 ? LastFeeValues.Select(x => BigInteger.Parse(x)).Average().ToString() : BigInteger.Zero.ToString();

    public int FeeTokenId { get; set; }

    public int TokenId { get; set; }

    public string[] LastFeeValues { get; set; } = [];

    public TransactionType TransactionType { get; set; }

    public Token Token { get; set; } = null!;

    public Token FeeToken { get; set; } = null!;    
}