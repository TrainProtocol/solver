namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteWithSolverDto : QuoteDto
{
    public string DestinationSolverAddress { get; set; } = null!;

    public string SourceContractAddress { get; set; } = null!;

    public string DestinationContractAddress { get; set; } = null!;

}
