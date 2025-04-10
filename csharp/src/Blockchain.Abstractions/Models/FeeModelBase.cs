using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Blockchain.Abstractions.Models;

public abstract class FeeModelBase
{
    public decimal Amount
    {
        get
        {
            return UnitConversion.Convert.FromWei(BigInteger.Parse(AmountInWei), Decimals);
        }
    }

    public abstract string AmountInWei { get; }

    public string Asset { get; set; } = null!;

    public int Decimals { get; set; }
}
