using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp;
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

        if(options.HashicorpKeyVaultUri == null)
        {
            throw new InvalidOperationException("Hashicorp Key Vault URI is not set.");
        }

        configureOptions?.Invoke(options);

        if (options.EnableKubernetesAuth)
        {
            if (string.IsNullOrEmpty(options.HashicorpKeyVaultK8sTokenPath))
            {
                throw new("Hashicorp Key Vault K8s token path is not set.");
            }
            if (!File.Exists(options.HashicorpKeyVaultK8sTokenPath))
            {
                throw new FileNotFoundException(
                    $"Hashicorp Key Vault K8s token file not found at path: {options.HashicorpKeyVaultK8sTokenPath}");
            }

            builder.Services.AddTransient<IVaultClient>(sp => {
                // Always read fresh rotated token from file path
                string jwt = File.ReadAllText(options.HashicorpKeyVaultK8sTokenPath);

                return new VaultClient(new VaultClientSettings(
                    options.HashicorpKeyVaultUri.ToString(),
                    new KubernetesAuthMethodInfo(
                        options.HashicorpKeyVaultK8sAppRole,
                        jwt)));
            });
        }
        else
        {
            builder.Services.AddSingleton<IVaultClient>(sp =>
                new VaultClient(new VaultClientSettings(
                        options.HashicorpKeyVaultUri.ToString(),
                        new TokenAuthMethodInfo(options.HashicorpKeyVaultToken))));
        }
         
        builder.Services.AddTransient<IPrivateKeyProvider, HashicorpKeyVaultPrivateKeyProvider>();

        return builder;
    }
}
