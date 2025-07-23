using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class SwapDto
{
    public string CommitId { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public TokenNetworkDto Source { get; set; } = null!;

    public BigInteger SourceAmount { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string SourceContractAddress { get; set; } = null!;

    public TokenNetworkDto Destination { get; set; } = null!;

    public BigInteger DestinationAmount { get; set; }

    public string DestinationAddress { get; set; } = null!;

    public string DestinationContractAddress { get; set; } = null!;

    public BigInteger FeeAmount { get; set; }

    public IEnumerable<TransactionDto> Transactions { get; set; } = [];
}
