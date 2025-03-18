using Train.Solver.Data.Entities;

namespace Train.Solver.API.Models;

public class NetworkModel
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string? ChainId { get; set; }

    public string Group { get; set; } = null!;

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public DateTimeOffset ListingDate { get; set; }

    public TokenModel NativeToken { get; set; } = null!;

    public bool IsTestnet{ get; set; } 
}

public class NetworkWithTokensModel : NetworkModel
{
    public List<TokenModel> Tokens { get; set; } = [];

    public virtual List<NodeModel> Nodes { get; set; } = new();

    public virtual List<ContractModel> Contracts { get; set; } = new();

    public virtual List<ManagedAccountModel> ManagedAccounts { get; set; } = new();
}

public class NodeModel
{
    public string Url { get; set; } = null!;

    public NodeType Type { get; set; }
}

public class ContractModel
{
    public ContarctType Type { get; set; }

    public string Address { get; set; } = null!;
}

public class ManagedAccountModel
{
    public string Address { get; set; } = null!;

    public AccountType Type { get; set; }
}
