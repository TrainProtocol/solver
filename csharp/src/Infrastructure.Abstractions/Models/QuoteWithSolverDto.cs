namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteWithSolverDto : QuoteDto
{
    public string SolverAddress { get; set; } = null!;

    public string ContractAddress { get; set; } = null!;
}
