using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp;

namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithHashicorpKeyVault(
        this TrainSolverBuilder builder) => builder.WithHashicorpKeyVault(configureOptions: null);

    private static TrainSolverBuilder WithHashicorpKeyVault(
        this TrainSolverBuilder builder,
        Action<HashicorpKeyVaultOptions>? configureOptions)
    {
        var options = new HashicorpKeyVaultOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if(options.HashcorpKeyVaultUri == null)
        {
            throw new InvalidOperationException("Hashicorp Key Vault URI is not set.");
        }

        configureOptions?.Invoke(options);

        // TODO: change auth method for k8s service account auth after deployment
        builder.Services.AddSingleton<IVaultClient>(sp => 
            new VaultClient(new VaultClientSettings(
                    options.HashcorpKeyVaultUri.ToString(),
                    new TokenAuthMethodInfo(vaultToken: options.HashcorpKeyVaultToken))));

        builder.Services.AddTransient<IPrivateKeyProvider, HashicorpKeyVaultPrivateKeyProvider>();

        return builder;
    }
}
