using System.Text.Json;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.EVM.Extensions;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Secret.AzureKeyVault;
using Train.Solver.Data.Npgsql.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
        builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables())
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddTrainSolver(hostContext.Configuration)
            .WithNpgsqlRepositories()
            .WithAzureKeyVault()
            .WithEVMWorkflows();
    })
    .Build();

var scope = host.Services.CreateScope();
var a = scope.ServiceProvider.GetRequiredService<WorkflowActivities>();

await a.StartSwapWorkflowAsync(JsonSerializer.Deserialize<HTLCCommitEventMessage>("{\r\n  \"TxId\": \"0xcafef41a38d1e580e630336c0ba614b4c385fdf456ffc5b362cbc06fff5d7ce0\",\r\n  \"Id\": \"0x52c6790957594eea11a6fdd63bffe4d51a53cd63796903e6e5ffeed406310d71\",\r\n  \"Amount\": 0.01020117,\r\n  \"AmountInWei\": \"10201170000000000\",\r\n  \"ReceiverAddress\": \"0x2330bc7d79f670f51546dcf5fd0eca6889a7ceb9\",\r\n  \"SourceNetwork\": \"OPTIMISM_SEPOLIA\",\r\n  \"SenderAddress\": \"0xB2029bbd8C1cBCC43c3A7b7fE3d118b0C57D7C31\",\r\n  \"SourceAsset\": \"ETH\",\r\n  \"DestinationAddress\": \"0xb2029bbd8c1cbcc43c3a7b7fe3d118b0c57d7c31\",\r\n  \"DestinationNetwork\": \"ETHEREUM_SEPOLIA\",\r\n  \"DestinationAsset\": \"ETH\",\r\n  \"TimeLock\": 1743685813,\r\n  \"DestinationNetworkType\": 0,\r\n  \"SourceNetworkType\": 0\r\n}" +
    ""));

await host.RunAsync();
