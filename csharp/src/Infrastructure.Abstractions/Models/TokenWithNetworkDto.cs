using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.API.Models;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TokenWithNetworkDto : TokenDto
{
    public NetworkDto Network { get; set; } = null!;
}
