using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateSignerAgentRequest
{
    public NetworkType[] SupportedTypes { get; set; } = [];
    public string Url { get; set; } = default!;
    public string Name { get; set; } = default!;
}

