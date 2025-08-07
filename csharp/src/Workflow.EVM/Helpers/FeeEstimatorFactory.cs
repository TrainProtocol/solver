using Train.Solver.Common.Enums;
using Train.Solver.SmartNodeInvoker;

namespace Train.Solver.Workflow.EVM.Helpers;

public interface IFeeEstimatorFactory
{
    IFeeEstimator Create(TransactionFeeType type);
}

public class FeeEstimatorFactory(ISmartNodeInvoker nodeInvoker) : IFeeEstimatorFactory
{
    public IFeeEstimator Create(TransactionFeeType type)
    {
        switch (type)
        {
            case TransactionFeeType.Default:
                return new EthereumLegacyFeeEstimator(nodeInvoker);
            case TransactionFeeType.EIP1559:
                return new EthereumEIP1559FeeEstimator(nodeInvoker);
            case TransactionFeeType.ArbitrumEIP1559:
                return new ArbitrumEIP1559FeeEstimator(nodeInvoker);
            case TransactionFeeType.OptimismEIP1559:
                return new OptimismEIP1559FeeEstimator(nodeInvoker);
            default:
                throw new NotSupportedException($"Fee estimator for {type} is not supported.");
        }
    }
}
