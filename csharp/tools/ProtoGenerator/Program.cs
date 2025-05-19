using Azure.Security.KeyVault.Secrets;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Data.Npgsql;
using Train.Solver.Data.Npgsql.Extensions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Secret.AzureKeyVault;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Local.json")
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();

var services = new ServiceCollection();

var trainBuilder = new TrainSolverBuilder(services, configuration, null)
    .WithNpgsqlRepositories()
    .WithAzureKeyVault();

var serviceProvider = services.BuildServiceProvider();

var solverDbContext = serviceProvider.GetRequiredService<SolverDbContext>();

var azureKeyVaultClient = serviceProvider.GetRequiredService<SecretClient>();

var k8sJwt = "YOUR_K8S_JWT";
var godToken = "YOUR_GOD_TOKEN";

var vaultProdUrl = "https://vault.lb.layerswap.io";

var writeVaultClient = new VaultClient(new VaultClientSettings(
                       vaultProdUrl,
                       new TokenAuthMethodInfo(godToken)));

var readVaultk8sClient = new VaultClient(new VaultClientSettings(
    vaultProdUrl,
    new KubernetesAuthMethodInfo(
        "train-reader",
        k8sJwt)));

var managedAccounts = await solverDbContext.ManagedAccounts.ToListAsync();

foreach (var address in managedAccounts.Select(x => x.Address).Distinct())
{
    var secret = await readVaultk8sClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: address,
            mountPoint: "secret");

    var a = secret.Data.Data["private_key"].ToString();
    var privateKey = (await azureKeyVaultClient.GetSecretAsync(address)).Value.Value;

    await writeVaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
        path: address,
        data: new Dictionary<string, object> { { "private_key", privateKey } },
        mountPoint: "secret");
}