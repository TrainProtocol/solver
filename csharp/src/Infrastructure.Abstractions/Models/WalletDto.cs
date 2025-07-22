using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class WalletDto
{
    public string Name { get; set; }

    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }
}
