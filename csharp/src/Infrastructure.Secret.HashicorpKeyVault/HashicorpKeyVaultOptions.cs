namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

public class HashicorpKeyVaultOptions
{
    public Uri HashcorpKeyVaultUri { get; set; }

    public string HashcorpKeyVaultToken { get; set; }

    public string MountPath { get; set; } = "secret";
}
