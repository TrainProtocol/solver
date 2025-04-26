namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public class HashicorpKeyVaultOptions
{
    public Uri HashcorpKeyVaultUri { get; set; }

    public string HashcorpKeyVaultToken { get; set; }

    // Only for K8s auth method
    public string? HashcorpKeyVaultK8sAppRole { get; set; }

    // Only for K8s auth method
    public string? HashcorpKeyVaultK8sTokenPath{ get; set; }

    public string HashcorpKeyVaultMountPath { get; set; } = "secret";

    public bool EnableKubernetesAuth { get; set; }
}
