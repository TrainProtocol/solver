using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Services;

namespace Train.Solver.Secret.AzureKeyVault;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithAzureKeyVault(
        this TrainSolverBuilder builder)
    {
        return builder.WithAzureKeyVault(null);
    }

    private static TrainSolverBuilder WithAzureKeyVault(this TrainSolverBuilder builder,
       Action<AzureKeyVaultOptions>? configureOptions)
    {
        var options = new AzureKeyVaultOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if(options.AzureKeyVaultUri == null)
        {
            throw new InvalidOperationException("Azure Key Vault URI is not set.");
        }

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
