using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class SignerAgent : EntityBase
{
    public string Name { get; set; } = null!;

    public NetworkType[] SupportedTypes { get; set; } = [];

    public string Url { get; set; } = null!;
}
