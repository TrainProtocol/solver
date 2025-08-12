using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ExtendedRouteDto
{
    public required ExtendedTokenNetworkDto Source { get; set; } = null!;

    public required ExtendedTokenNetworkDto Destination { get; set; } = null!;
}
