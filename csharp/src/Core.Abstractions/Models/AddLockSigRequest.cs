namespace Train.Solver.Core.Abstractions.Models;

public class AddLockSignatureRequest : AddLockSignatureModel
{
    public required string Id { get; set; } = null!;

    public required string Hashlock { get; set; } = null!;

    public required string SignerAddress { get; set; } = null!;

    public required string Asset { get; set; } = null!;

    public required string NetworkName { get; set; } = null!;
}

public class AddLockSignatureModel
{
    public string? R { get; set; }

    public string? S { get; set; }

    public string? V { get; set; }

    public string? Signature { get; set; }

    public string[]? SignatureArray { get; set; }

    public required long Timelock { get; set; }
}
