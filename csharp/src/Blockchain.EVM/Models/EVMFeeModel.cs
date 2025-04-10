using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.EVM.Models;

public class EVMFeeModel : FeeModelBase
{
    public EIP1559Data? Eip1559FeeData { get; set; }

    public LegacyData? LegacyFeeData { get; set; }

    public override string AmountInWei
    {
        get
        {
            BigInteger amount = default;

            if (LegacyFeeData != null)
            {
                amount =  BigInteger.Multiply(
                    BigInteger.Parse(LegacyFeeData!.GasPriceInWei),
                    BigInteger.Parse(LegacyFeeData.GasLimit));

            }else if(Eip1559FeeData != null)
            {
                amount =
                    BigInteger.Multiply(
                    BigInteger.Parse(Eip1559FeeData!.MaxFeePerGasInWei),
                    BigInteger.Parse(Eip1559FeeData.GasLimit));

                if (Eip1559FeeData.L1FeeInWei != null)
                {
                    amount += BigInteger.Parse(Eip1559FeeData.L1FeeInWei);
                }
            }

            return amount.ToString();
        }
    }
}

public abstract class EVMFeeDataBase(string gasLimit, string? l1FeeInWei)
{
    public string GasLimit { get; set; } = gasLimit;

    public string? L1FeeInWei { get; set; } = l1FeeInWei;
}

public class EIP1559Data : EVMFeeDataBase
{
    public EIP1559Data(
        string maxPriorityFeeInWei,
        string baseFeeInWei,
        string gasLimit,
        string? l1FeeInWei = null) : base(gasLimit, l1FeeInWei)
    {
        MaxPriorityFeeInWei = maxPriorityFeeInWei;
        BaseFeeInWei = baseFeeInWei;
        L1FeeInWei = l1FeeInWei;
    }

    public string MaxFeePerGasInWei
    {
        get
        {
            return (BigInteger.Parse(MaxPriorityFeeInWei) + BigInteger.Parse(BaseFeeInWei)).ToString();
        }
    }

    public string MaxPriorityFeeInWei { get; set; }

    public string BaseFeeInWei { get; set; }
}

public class LegacyData(string gasPriceInWei,
    string gasLimit,
    string? l1FeeInWei = null) : EVMFeeDataBase(gasLimit, l1FeeInWei)
{
    public string GasPriceInWei { get; set; } = gasPriceInWei;
}

