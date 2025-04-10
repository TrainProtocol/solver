using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaFeeModel : FeeModelBase
{
    public decimal ComputeUnitPrice { get; set; }

    public decimal ComputeUnitLimit { get; set; } 

    public decimal BaseFee { get; set; }

    public override string AmountInWei
    {
        get
        {
            return (BaseFee + ComputeUnitPrice * ComputeUnitLimit).ToString();
        }
    }
}
