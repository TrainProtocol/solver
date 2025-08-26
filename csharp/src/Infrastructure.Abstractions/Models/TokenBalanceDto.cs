using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TokenBalanceDto
{
    public required TokenDto Token { get; set; }

    public required BigInteger Amount { get; set; }
}
