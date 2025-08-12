using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ExtendedTokenNetworkDto : TokenNetworkDto
{
    public new ExtendedNetworkDto Network { get; set; } = null!;
}
