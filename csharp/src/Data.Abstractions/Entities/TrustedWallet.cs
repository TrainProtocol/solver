using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class TrustedWallet : EntityBase
{
    public string Name { get; set; }

    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }
}
