using Solnet.Programs.Utilities;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace Train.Solver.Core.Blockchains.Solana.Programs;

public static class ComputeBudgetProgram
{
    public static readonly PublicKey ProgramIdKey = new("ComputeBudget111111111111111111111111111111");
    private const string ProgramName = "Compute Budget";

    public static TransactionInstruction SetComputeUnitLimit(uint units)
    {
        return new TransactionInstruction
        {
            ProgramId = ProgramIdKey.KeyBytes,
            Keys = [],
            Data = EncodeSetComputeUnitLimitData(units)
        };
    }

    public static TransactionInstruction SetComputeUnitPrice(ulong microLamports)
    {
        return new TransactionInstruction
        {
            ProgramId = ProgramIdKey.KeyBytes,
            Keys = [],
            Data = EncodeSetComputeUnitPriceData(microLamports)
        };
    }

    public static byte[] EncodeSetComputeUnitLimitData(uint units)
    {
        byte[] data = new byte[5];

        data.WriteU8(2, 0);
        data.WriteU32(units, 1);
     
        return data;
    }

    public static byte[] EncodeSetComputeUnitPriceData(ulong microLamports)
    {
        byte[] data = new byte[9];

        data.WriteU8(3, 0);
        data.WriteU64(microLamports, 1);

        return data;
    }
}
