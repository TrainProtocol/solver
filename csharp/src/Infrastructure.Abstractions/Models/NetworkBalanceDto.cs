using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Infrastructure.Abstractions.Models;
public class NetworkBalanceDto
{
    public required ExtendedNetworkDto Network { get; set; }

    public required List<TokenBalanceDto> Balances { get; set; }
}
