using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.UserPass;

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

        if (options.HashicorpEnableKubernetesAuth)
        {
            var k8sServiceAccountTokenPath = Environment.GetEnvironmentVariable("K8S_SERVICE_ACCOUNT_TOKEN_PATH");

            if (string.IsNullOrEmpty(k8sServiceAccountTokenPath))
            {
                throw new("Hashicorp Key Vault K8s token path is not set.");
            }
            if (!File.Exists(k8sServiceAccountTokenPath))
            {
                throw new FileNotFoundException(
                    $"Hashicorp Key Vault K8s token file not found at path: {k8sServiceAccountTokenPath}");
            }

            builder.Services.AddTransient<IVaultClient>(sp => {
                // Always read fresh rotated token from file path
                string jwt = File.ReadAllText(k8sServiceAccountTokenPath);

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
                        new UserPassAuthMethodInfo(
                            options.HashicorpKeyVaultUsername, 
                            options.HashicorpKeyVaultPassword))));
        }
         
        builder.Services.AddTransient<IPrivateKeyProvider, HashicorpKeyVaultPrivateKeyProvider>();

        return builder;
    }
}
