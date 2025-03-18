namespace Train.Solver.Core.Blockchain.Models;

public class PrepareTransactionResponse
{
    public string ToAddress { get; set; } = null!;

    public string? Data { get; set; }

    public decimal Amount { get; set; }

    public string Asset { get; set; }

    public string AmountInWei { get; set; } = null!;

    public string CallDataAsset { get; set; } = null!;

    public string CallDataAmountInWei { get; set; } = null!;

    public decimal CallDataAmount { get; set; } 
}
