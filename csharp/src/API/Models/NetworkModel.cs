using System.Text.Json.Serialization;
using Train.Solver.Data.Entities;

namespace Train.Solver.API.Models;

public class NetworkModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = null!;

    [JsonPropertyName("logo")]
    public string Logo { get; set; } = null!;

    [JsonPropertyName("chain_id")]
    public string? ChainId { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; } = null!;

    [JsonPropertyName("transaction_explorer_template")]
    public string TransactionExplorerTemplate { get; set; } = null!;

    [JsonPropertyName("account_explorer_template")]
    public string AccountExplorerTemplate { get; set; } = null!;

    [JsonPropertyName("listing_date")]
    public DateTimeOffset ListingDate { get; set; }

    [JsonPropertyName("native_token")]
    public TokenModel NativeToken { get; set; } = null!;

    [JsonPropertyName("is_testnet")]
    public bool IsTestnet{ get; set; } 
}

public class NetworkWithTokensModel : NetworkModel
{
    [JsonPropertyName("tokens")]
    public List<TokenModel> Tokens { get; set; } = [];

    [JsonPropertyName("nodes")]
    public virtual List<NodeModel> Nodes { get; set; } = new();

    [JsonPropertyName("contracts")]
    public virtual List<ContractModel> Contracts { get; set; } = new();

    [JsonPropertyName("managed_accounts")]
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
