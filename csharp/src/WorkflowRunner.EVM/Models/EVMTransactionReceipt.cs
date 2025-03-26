using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;

namespace Train.Solver.Blockchains.EVM.Models;

public class EVMTransactionReceipt : TransactionReceipt
{
    [JsonProperty("l1Fee")]
    public HexBigInteger? L1Fee { get; set; }
}
