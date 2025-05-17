namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public class HashicorpKeyVaultOptions
{
    public Uri HashicorpKeyVaultUri { get; set; } = null!;

    // For UserPass auth method when HashicorpEnableKubernetesAuth is false
    public string? HashicorpKeyVaultUsername { get; set; }

    // For UserPass auth method when HashicorpEnableKubernetesAuth is false
    public string? HashicorpKeyVaultPassword { get; set; }

    // For K8s auth method when HashicorpEnableKubernetesAuth is true
    public string? HashicorpKeyVaultK8sAppRole { get; set; }

    public string HashicorpKeyVaultMountPath { get; set; } = "secret";

    public bool HashicorpEnableKubernetesAuth { get; set; }
}
