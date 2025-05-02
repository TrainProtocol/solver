using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDto
{
    public TokenNetworkDto Source { get; set; } = null!;

    public TokenNetworkDto Destionation { get; set; } = null!;
}
