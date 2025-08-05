namespace Train.Solver.AdminAPI.Models;

public class CreateNodeRequest
{
    public string ProviderName { get; set; } = null!;
    public string Url { get; set; } = null!;
}
