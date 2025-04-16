namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedTokenDto : TokenDto
{
    public string Logo { get; set; } = null!;

    public DateTimeOffset ListingDate { get; set; }
}
