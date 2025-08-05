using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Npgsql;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;

var options = new DbContextOptionsBuilder<SolverDbContext>()
            .UseNpgsql("Server=dev-ls-psqlserver.postgres.database.azure.com;Database=train_solver;Port=5432;User Id=dbadmin;Password=root41best4U%%;Ssl Mode=Require;Trust Server Certificate=true;").Options;

using var context = new SolverDbContext(options);

if (await context.Networks.AnyAsync())
{
    Console.WriteLine("🚫 Data already exists. Skipping seed.");
    return;
}

var ethereumNetwork = new Network
{
    Id = 1,
    Name = "ETHEREUM_SEPOLIA",
    DisplayName = "Ethereum Sepolia",
    Type = NetworkType.EVM,
    FeeType = TransactionFeeType.EIP1559,
    ChainId = "11155111",
    FeePercentageIncrease = default,
    HTLCNativeContractAddress = "0x72af39a799f31ef364f0c9e55b919149b9beb66f",
    HTLCTokenContractAddress = "0xc31b8b4232792133cfa6f0decc0509d44dcc3fd7",
    Nodes = [
    new Node
        {
            Id = 1,
            Url = "https://ethereum-sepolia-rpc.publicnode.com"
        }
    ]
};

var arbitrumNetwork = new Network
{
    Id = 2,
    Name = "ARBITRUM_SEPOLIA",
    DisplayName = "Arbitrum Sepolia",
    Type = NetworkType.EVM,
    FeeType = TransactionFeeType.ArbitrumEIP1559,
    ChainId = "421614",
    FeePercentageIncrease = default,
    HTLCNativeContractAddress = "0x343b08e7c4822f4b29842aed358d0879839eea61",
    HTLCTokenContractAddress = "0x2f68c9ec7c9d6ab155d6cabcf916a3bbb79b8e62",
    Nodes = [
        new Node
        {
            Id = 2,
            Url = "https://arbitrum-sepolia-rpc.publicnode.com"
        }
    ]
};

var accounts = new[]
{
    new Wallet
    {
        Id = 1,
        Address = "0x2330bc7d79f670f51546dcf5fd0eca6889a7ceb9",
        NetworkType = NetworkType.EVM,
        Name = "Default"
    },
};

var serviceFee = new ServiceFee
{
    Id = 1,
    FeePercentage = 0.01m,
    FeeInUsd = 0.5m,
};

context.ServiceFees.Add(serviceFee);
context.Networks.AddRange(ethereumNetwork, arbitrumNetwork);
context.Wallets.AddRange(accounts);
await context.SaveChangesAsync();

var rateProvider = new RateProvider
{
    Id = 1,
    Name = "SameAsset"
};

var tokenPrice = new TokenPrice
{
    Id = 1,
    PriceInUsd = 2000.0m,
    ExternalId = "ethereum",
    LastUpdated = DateTimeOffset.UtcNow
};

var ethereumNativeToken = new Token
{
    Id = 1,
    Asset = "ETH",
    Decimals = 18,
    TokenContract = null,
    TokenPrice = tokenPrice,
    NetworkId = 1
};

var arbitrumNativeToken = new Token
{
    Id = 2,
    Asset = "ETH",
    Decimals = 18,
    TokenContract = null,
    TokenPrice = tokenPrice,
    NetworkId = 2
};

var routes = new[]
{
    new Route
    {
        Id = 1,
        SourceTokenId = 1,
        DestinationTokenId = 2,
        RateProviderId = rateProvider.Id,
        MinAmountInSource = TokenUnitHelper.ToBaseUnits(0.000001m, 18).ToString(),
        MaxAmountInSource = TokenUnitHelper.ToBaseUnits(1.0m, 18).ToString(),
        Status = RouteStatus.Active,
        SourceWalletId = accounts[0].Id,
        DestinationWalletId = accounts[0].Id,
        ServiceFeeId = serviceFee.Id
    },
    new Route
    {
        Id = 2,
        SourceTokenId = 2,
        DestinationTokenId = 1,
        RateProviderId = rateProvider.Id,
        MinAmountInSource = TokenUnitHelper.ToBaseUnits(0.000001m, 18).ToString(),
        MaxAmountInSource = TokenUnitHelper.ToBaseUnits(1.0m, 18).ToString(),
        Status = RouteStatus.Active,
        SourceWalletId = accounts[0].Id,
        DestinationWalletId = accounts[0].Id,
        ServiceFeeId = serviceFee.Id
    }
};

ethereumNetwork.NativeTokenId = ethereumNativeToken.Id;
arbitrumNetwork.NativeTokenId = arbitrumNativeToken.Id;

await context.AddRangeAsync([
    rateProvider,
    tokenPrice,
    ethereumNativeToken,
    arbitrumNativeToken,
    ..routes,
]);

await context.SaveChangesAsync();
Console.WriteLine("✅ Seed completed.");