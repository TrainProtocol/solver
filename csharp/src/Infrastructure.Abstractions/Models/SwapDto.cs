using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class SwapDto
{
    public int Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string CommitId { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public ExtendedTokenNetworkDto Source { get; set; } = null!;

    public BigInteger SourceAmount { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string SourceContractAddress { get; set; } = null!;

    public ExtendedTokenNetworkDto Destination { get; set; } = null!;

    public BigInteger DestinationAmount { get; set; }

    public string DestinationAddress { get; set; } = null!;

    public string DestinationContractAddress { get; set; } = null!;

    public BigInteger FeeAmount { get; set; }

    public IEnumerable<TransactionDto> Transactions { get; set; } = [];
}

public class DetailedSwapDto : SwapDto
{
    public DetailedWalletDto SourceWallet { get; set; } = null!;

    public DetailedWalletDto DestinationWallet { get; set; } = null!;
}
