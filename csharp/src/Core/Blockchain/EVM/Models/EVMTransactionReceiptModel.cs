using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;

namespace Train.Solver.Core.Blockchain.EVM.Models;

public class EVMTransactionReceiptModel : TransactionReceipt
{
    [JsonProperty("l1Fee")]
    public HexBigInteger L1Fee { get; set; }
}
