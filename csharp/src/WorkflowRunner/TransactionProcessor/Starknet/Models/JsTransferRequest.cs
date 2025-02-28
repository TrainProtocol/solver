namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsTransferRequest
{
    public string CorrelationId { get; set; } = null!;

    public string ReferenceId { get; set; } = null!;

    public string FromAddress { get; set; } = null!;

    public string ToAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Nonce { get; set; }

    public string Network { get; set; }

    public string TokenContract { get; set; }

    public string ChainId { get; set; }

    public int Decimals { get; set; }

    public string EthereumNodeUrl { get; set; }

    public string NodeUrl { get; set; }

    public string FeeAsset { get; set; }

    public string FeeTokenContract { get; set; }

    public string FeeAmountInWei { get; set; }

    public string? Memo { get; set; }

    public string? CallData { get; set; }
}
