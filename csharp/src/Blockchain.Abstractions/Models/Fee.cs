using System.Numerics;
using Nethereum.Util;

namespace Train.Solver.Blockchain.Abstractions.Models;

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

    public decimal Amount
    {
        get
        {
            if (FixedFeeData != null)
            {
                return UnitConversion.Convert.FromWei(BigInteger.Parse(FixedFeeData.FeeInWei), Decimals);
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

    public string AmountInWei
    {
        get
        {
            return UnitConversion.Convert.ToWei(Amount, Decimals).ToString();
        }
    }

    public string Asset { get; set; } = null!;

    public int Decimals { get; set; }

    public EIP1559Data? Eip1559FeeData { get; set; }

    public FixedFeeData? FixedFeeData { get; set; }

    public LegacyData? LegacyFeeData { get; set; }

    public SolanaFeeData? SolanaFeeData { get; set; }

    private decimal CalculateLegacyFeeAmount()
    {
        var amount = BigInteger.Multiply(
                       BigInteger.Parse(LegacyFeeData!.GasPriceInWei),
                                  BigInteger.Parse(LegacyFeeData.GasLimit));

        if (LegacyFeeData.L1FeeInWei != null)
        {
            amount += BigInteger.Parse(LegacyFeeData.L1FeeInWei);
        }

        return UnitConversion.Convert.FromWei(amount, Decimals);
    }

    private decimal CalculateEip1559Amount()
    {
        var amount =
            BigInteger.Multiply(
                BigInteger.Parse(Eip1559FeeData!.MaxFeePerGasInWei),
                BigInteger.Parse(Eip1559FeeData.GasLimit));

        if (Eip1559FeeData.L1FeeInWei != null)
        {
            amount += BigInteger.Parse(Eip1559FeeData.L1FeeInWei);
        }

        return UnitConversion.Convert.FromWei(amount, Decimals);
    }

    private decimal CalculateSolanaFeeAmount()
    {
        var amount =
            decimal.Parse(SolanaFeeData!.BaseFee) +
            decimal.Parse(SolanaFeeData!.ComputeUnitPrice) *
            decimal.Parse(SolanaFeeData.ComputeUnitLimit);

        return UnitConversion.Convert.FromWei(new BigInteger(amount), Decimals);
    }
}

public class SolanaFeeData(string computeUnitPrice, string computeUnitLimit, string baseFee)
{
    public string ComputeUnitPrice { get; set; } = computeUnitPrice;
    public string ComputeUnitLimit { get; set; } = computeUnitLimit;
    public string BaseFee { get; set; } = baseFee;
}

public class FixedFeeData(string feeInWei)
{
    public string FeeInWei { get; set; } = feeInWei;
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
