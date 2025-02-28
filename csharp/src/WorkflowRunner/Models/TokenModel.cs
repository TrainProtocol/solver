namespace Train.Solver.WorkflowRunner.Models;

public class TokenModel
{
    public int Id { get; set; }

    public string NetworkName { get; set; } = null!;

    public int NetworkId { get; set; }

    public string Asset { get; set; } = null!;

    public int Precision { get; set; }

    public bool IsTestnet { get; set; }
}
