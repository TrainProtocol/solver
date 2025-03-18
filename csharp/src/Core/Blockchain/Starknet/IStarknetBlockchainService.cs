using FluentResults;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;

namespace Train.Solver.Core.Blockchain.Starknet;

public interface IStarknetBlockchainService : IBlockchainService
{
    Result<TransactionStatuses> ValidateTransactionStatus(string finalityStatus, string executionStatus);
}
