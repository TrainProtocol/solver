using Train.Solver.Util.Enums;

namespace Train.Solver.Blockchain.EVM.Helpers;

public static class FeeEstimatorFactory
{
    public static IFeeEstimator Create(TransactionFeeType type)
    {
        switch (type)
        {
            case TransactionFeeType.Default:
                return new EthereumLegacyFeeEstimator();
            case TransactionFeeType.EIP1559:
                return new EthereumEIP1559FeeEstimator();
            case TransactionFeeType.ArbitrumEIP1559:
                return new ArbitrumEIP1559FeeEstimator();
            case TransactionFeeType.OptimismEIP1559:
                return new OptimismEIP1559FeeEstimator();
            default:
                throw new NotSupportedException($"Fee estimator for {type} is not supported.");
        }
    }
}
