using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public enum NetworkGroup
{
    EVMEthereumLegacy,
    EVMEthereumEip1559,
    EVMArbitrumLegacy,
    EVMArbitrumEip1559,
    EVMOptimismEip1559,
    EVMOptimismLegacy,
    EVMPolygonLegacy,
    EVMPolygonEip1559,
    Solana,
    Starknet,
}

public class Network : EntityBase<int>
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public NetworkGroup Group { get; set; }

    public string? ChainId { get; set; }

    public int FeePercentageIncrease { get; set; }

    public int? GasLimitPercentageIncrease { get; set; }

    public string? FixedGasPriceInGwei { get; set; }

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public bool IsTestnet { get; set; }

    public int ReplacementFeePercentage { get; set; }

    public bool IsExternal { get; set; }

    public string Logo { get; set; }

    public virtual List<ManagedAccount> ManagedAccounts { get; set; } = new();

    public virtual List<Token> Tokens { get; set; } = new();

    public virtual List<Node> Nodes { get; set; } = new();
    
    public virtual List<Contract> DeployedContracts { get; set; } = new();

}
