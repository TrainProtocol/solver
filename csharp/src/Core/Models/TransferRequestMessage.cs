namespace Train.Solver.Core.Models;

public class TransferRequestMessage : TransferRequestBase
{
    public string NetworkName { get; set; } = null!;

    public string? Nonce { get; set; }

    public string? CallData { get; set; }

    public Fee? Fee { get; set; }
}
