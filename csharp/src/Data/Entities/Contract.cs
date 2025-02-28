using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public enum ContarctType
{
    HTLCNativeContractAddress,
    HTLCTokenContractAddress,
    GasPriceOracleContract,
    ZKSPaymasterContract,
    EvmMultiCallContract,
    EvmOracleContract,
    WatchdogContractAddress
}


public class Contract : EntityBase<int>
{
    public ContarctType Type { get; set; }

    public string Address { get; set; } = null!;

    public virtual List<Network> Networks { get; set; } = [];
}
