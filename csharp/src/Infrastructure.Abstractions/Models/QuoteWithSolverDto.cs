using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteWithSolverDto : QuoteDto
{
    public string SolverAddressInSource { get; set; } = null!;

    public string NativeContractAddressInSource { get; set; } = null!;

    public string TokenContractAddressInSource { get; set; } = null!;
}
