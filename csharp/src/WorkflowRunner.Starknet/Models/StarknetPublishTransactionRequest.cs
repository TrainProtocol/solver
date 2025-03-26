using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.Starknet.Models;

public class StarknetPublishTransactionRequest : BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public string CallData { get; set; } = null!;

    public string Nonce { get; set; } = null!;

    public Fee Fee { get; set; } = null!;
}
