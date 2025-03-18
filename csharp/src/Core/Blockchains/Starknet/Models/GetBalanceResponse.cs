using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;

namespace Train.Solver.Core.Blockchains.Starknet.Models;

public class GetBalanceResponse
{
    [JsonProperty("result")]
    public HexBigInteger[] Result { get; set; }
}