using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;

namespace Train.Solver.Core.Blockchain.Starknet.Models;

public class GetBalanceResponse
{
    [JsonProperty("result")]
    public HexBigInteger[] Result { get; set; }
}