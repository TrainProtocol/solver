using FluentResults;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Errors;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Solana;

public interface ISolanaBlockchainService : IBlockchainService
{
     Task<Result<BaseError>> CheckBlockHeightAsync(
        Network network,
        string fromAddress);
}
