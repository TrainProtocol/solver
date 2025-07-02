using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteWithSolverDto : QuoteDto
{
    public string SolverAddress { get; set; } = null!;

    public string ContractAddress { get; set; } = null!;
}
