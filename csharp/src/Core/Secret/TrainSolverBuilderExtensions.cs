using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.Core.Secret;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder AddAzureKeyVaultStorage(
        this TrainSolverBuilder builder,
        IConfiguration configuration)
    {
        return builder.AddAzureKeyVaultStorage(configuration, null);
    }

    public static TrainSolverBuilder AddAzureKeyVaultStorage(this TrainSolverBuilder builder,
       IConfiguration configuration,
       Action<AzureKeyVaultOptions>? configureOptions)
    {
        var options = new AzureKeyVaultOptions();
        configuration.GetSection("TrainSolver").Bind(options);

        configureOptions?.Invoke(options);

        builder.Services.AddAzureClients(builder =>
        {
            builder
                .AddSecretClient(options.AzureKeyVaultUri)
                .WithCredential(new DefaultAzureCredential());
        });

        builder.Services.AddTransient<IPrivateKeyProvider, AzureKeyVaultPrivateKeyProvider>();

        return builder;
    }
}
