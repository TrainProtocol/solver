using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public enum ContarctType
{
    HTLCNativeContractAddress,
    HTLCTokenContractAddress,
    GasPriceOracleContract,
    EvmMultiCallContract,
}


public class Contract : EntityBase<int>
{
    public ContarctType Type { get; set; }

    public string Address { get; set; } = null!;

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
