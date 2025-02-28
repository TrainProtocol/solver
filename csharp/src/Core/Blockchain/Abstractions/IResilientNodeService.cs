using System.Numerics;
using FluentResults;
using Nethereum.Hex.HexTypes;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Abstractions;

public interface IResilientNodeService
{
    Task<Result<T>> GetDataFromNodesAsync<T>(IEnumerable<Node> nodes, Func<string, Task<T>> dataRetrievalTask);

    Task<Result<BigInteger>> GetGasLimitAsync(ICollection<Node> nodes, string fromAddress, string toAddress, Token currency, decimal? amount = null, string? callData = null);
    
    Task<Result<HexBigInteger>> GetGasPriceAsync(ICollection<Node> nodes);
    
    Task<Result<EVMTransactionReceiptModel>> GetTransactionReceiptAsync(ICollection<Node> nodes, string transactionHash);
}