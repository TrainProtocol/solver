using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public enum NetworkType
{
    EVM,
    Solana,
    Starknet,
}

public enum TransactionFeeType
{
    Default,
    EIP1559,
    ArbitrumEIP1559,
    OptimismEIP1559,
}

public class Network : EntityBase<int>
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public NetworkType Type { get; set; }

    public TransactionFeeType FeeType { get; set; }

    public string? ChainId { get; set; }

    public int FeePercentageIncrease { get; set; }

    public int? GasLimitPercentageIncrease { get; set; }

    public string? FixedGasPriceInGwei { get; set; }

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public bool IsTestnet { get; set; }

    public int ReplacementFeePercentage { get; set; }

    public bool IsExternal { get; set; }

    public string Logo { get; set; } = null!;

    public int? NativeTokenId { get; set; }

    public virtual Token? NativeToken { get; set; }

    public virtual List<ManagedAccount> ManagedAccounts { get; set; } = new();

    public virtual List<Token> Tokens { get; set; } = new();

    public virtual List<Node> Nodes { get; set; } = new();

    public virtual List<Contract> Contracts { get; set; } = new();

}
