using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class SignerAgentDto
{
    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public NetworkType[] SupportedTypes { get; set; } = null!;
}
