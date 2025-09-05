using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class Workspace : EntityBase
{
    public string Name { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public string SignerAgentUrl { get; set; } = null!;

    public NetworkType[] SupportedNetworkTypes { get; set; } = [];

    public List<Network> Networks { get; set; } = [];

    public List<Route> Routes { get; set; } = [];

    public List<Swap> Swaps { get; set; } = [];

    public List<SwapMetric> SwapMetrics { get; set; } = [];

    public List<Transaction> Transactions { get; set; } = [];

    public List<Wallet> Wallets { get; set; } = [];

    public List<TrustedWallet> TrustedWallets { get; set; } = [];

    public List<Expense> Expenses { get; set; } = [];

    public List<ServiceFee> ServiceFees { get; set; } = [];
}
