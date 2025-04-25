using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Kubernetes;

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

        IAuthMethodInfo? authMethodInfo = null;

        if (options.EnableKubernetesAuth)
        {
            // TODO: add auth method for k8s service account and agent injector
            authMethodInfo = new KubernetesAuthMethodInfo(
                options.HashcorpKeyVaultK8sAppRole, 
                options.HashcorpKeyVaultK8sJWT);
        }
        else
        {
            authMethodInfo = new TokenAuthMethodInfo(options.HashcorpKeyVaultToken);
        }

        builder.Services.AddSingleton<IVaultClient>(sp =>
            new VaultClient(new VaultClientSettings(
                    options.HashcorpKeyVaultUri.ToString(),
                    authMethodInfo)));

        builder.Services.AddTransient<IPrivateKeyProvider, HashicorpKeyVaultPrivateKeyProvider>();

        return builder;
    }
}
