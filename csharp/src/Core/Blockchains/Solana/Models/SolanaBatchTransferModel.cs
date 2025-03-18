namespace Train.Solver.Core.Blockchains.Solana.Models;

public class SolanaBatchTransferModel
{
    public string Currency { get; set; } = null!;

    public string ToAddress { get; set; } = null!;

    public override bool Equals(object? obj)
        => obj is not null
            && obj is SolanaBatchTransferModel batchTransferModel
            && Currency.Equals(batchTransferModel.Currency)
            && ToAddress.Equals(batchTransferModel.ToAddress);

    public override int GetHashCode() 
        => HashCode.Combine(Currency, ToAddress);
}
