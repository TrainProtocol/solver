using System.Numerics;
using Nethereum.Util;

namespace Train.Solver.Workflow.Abstractions.Models;

public class Fee
{
    public Fee()
    {
            
    }

    public Fee(string feeAsset, int decimals)
    {
        Asset = feeAsset;
        Decimals = decimals;
    }

    public Fee(string asset, int decimals, EIP1559Data feeData) : this(asset, decimals)
    {
        Eip1559FeeData = feeData;
    }

    public Fee(string asset, int decimals, LegacyData feeData) : this(asset, decimals)
    {
        LegacyFeeData = feeData;
    }

    public Fee(string asset, int decimals, FixedFeeData feeData) : this(asset, decimals)
    {
        FixedFeeData = feeData;
    }

    public Fee(string asset, int decimals, SolanaFeeData feeData) : this(asset, decimals)
    {
        SolanaFeeData = feeData;
    }

    public BigInteger Amount
    {
        get
        {
            if (FixedFeeData != null)
            {
                return FixedFeeData.Fee;
            }
            else if (Eip1559FeeData != null)
            {
                return CalculateEip1559Amount();
            }
            else if (LegacyFeeData != null)
            {
                return CalculateLegacyFeeAmount();
            }
            else if (SolanaFeeData != null)
            {
                return CalculateSolanaFeeAmount();
            }

            return default;
        }
    }

    public string Asset { get; set; } = null!;

    public int Decimals { get; set; }

    public EIP1559Data? Eip1559FeeData { get; set; }

    public FixedFeeData? FixedFeeData { get; set; }

    public LegacyData? LegacyFeeData { get; set; }

    public SolanaFeeData? SolanaFeeData { get; set; }

    private BigInteger CalculateLegacyFeeAmount()
    {
        var amount = BigInteger.Multiply(
                       LegacyFeeData!.GasPrice,
                       LegacyFeeData.GasLimit);

        if (LegacyFeeData.L1Fee != null)
        {
            amount += LegacyFeeData.L1Fee.Value;
        }

        return amount;
    }

    private BigInteger CalculateEip1559Amount()
    {
        var amount =
            BigInteger.Multiply(
                Eip1559FeeData!.MaxFeePerGas,
                Eip1559FeeData.GasLimit);

        if (Eip1559FeeData.L1Fee != null)
        {
            amount += Eip1559FeeData.L1Fee.Value;
        }

        return amount;
    }

    private BigInteger CalculateSolanaFeeAmount()
    {
        var amount =
            decimal.Parse(SolanaFeeData!.BaseFee) +
            decimal.Parse(SolanaFeeData!.ComputeUnitPrice) *
            decimal.Parse(SolanaFeeData.ComputeUnitLimit);

        return new BigInteger(amount);
    }
}

public class SolanaFeeData(string computeUnitPrice, string computeUnitLimit, string baseFee)
{
    public string ComputeUnitPrice { get; set; } = computeUnitPrice;
    public string ComputeUnitLimit { get; set; } = computeUnitLimit;
    public string BaseFee { get; set; } = baseFee;
}

public class FixedFeeData(BigInteger fee)
{
    public BigInteger Fee { get; set; } = fee;
}

public abstract class EVMFeeDataBase(BigInteger gasLimit, BigInteger? l1Fee)
{
    public BigInteger GasLimit { get; set; } = gasLimit;

    public BigInteger? L1Fee { get; set; } = l1Fee;
}

public class EIP1559Data : EVMFeeDataBase
{
    public EIP1559Data(
        BigInteger maxPriorityFee,
        BigInteger baseFee,
        BigInteger gasLimit,
        BigInteger? l1Fee = null) : base(gasLimit, l1Fee)
    {
        MaxPriorityFee = maxPriorityFee;
        BaseFee = baseFee;
        L1Fee = l1Fee;
    }

    public BigInteger MaxFeePerGas
    {
        get
        {
            return MaxPriorityFee + BaseFee;
        }
    }

    public BigInteger MaxPriorityFee { get; set; }

    public BigInteger BaseFee { get; set; }
}

public class LegacyData(BigInteger gasPrice,
    BigInteger gasLimit,
    BigInteger? l1Fee = null) : EVMFeeDataBase(gasLimit, l1Fee)
{
    public BigInteger GasPrice { get; set; } = gasPrice;
}
