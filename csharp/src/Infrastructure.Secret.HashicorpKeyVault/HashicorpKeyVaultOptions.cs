namespace Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

// TODO: Add deployed/compose url
public class HashicorpKeyVaultOptions
{
    public Uri HashcorpKeyVaultUri { get; set; } = new("http://127.0.0.1:8200");

    public string HashcorpKeyVaultToken { get; set; } = "dev-only-token";

    public string MountPath { get; set; } = "secret";
}
