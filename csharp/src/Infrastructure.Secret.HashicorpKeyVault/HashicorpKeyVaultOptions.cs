namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public class HashicorpKeyVaultOptions
{
    public Uri HashicorpKeyVaultUri { get; set; }

    public string HashicorpKeyVaultToken { get; set; }

    // Only for K8s auth method
    public string? HashicorpKeyVaultK8sAppRole { get; set; }

    // Only for K8s auth method
    public string? HashicorpKeyVaultK8sTokenPath{ get; set; }

    public string HashicorpKeyVaultMountPath { get; set; } = "secret";

    public bool HashicorpEnableKubernetesAuth { get; set; }
}
