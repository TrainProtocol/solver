using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram;

public static class HtlcSolProgram
{
    public static TransactionBuilder SetSolLockTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSolLockRequest htlcLockRequest)
    {
        var pdaParams = GetSolHtlcPdaParams(htlcLockRequest.Id, htlcProgramIdKey);

        var lockData = new HtlcSolInstructionDataBuilder().CreateSolLockData(htlcLockRequest);
        var lockRewardData = new HtlcSolInstructionDataBuilder().CreateLockRewardData(htlcLockRequest);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSolInstructionKeyProvider.CreateSolLockAccountKeys(htlcLockRequest, pdaParams),
            Data = lockData
        });

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSolInstructionKeyProvider.CreateSolLockRewardAccountKeys(htlcLockRequest, pdaParams),
            Data = lockRewardData
        });
        return builder;
    }

    public static TransactionBuilder SetSolRedeemTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSolRedeemRequest redeemRequest)
    {
        var pdaParams = GetSolHtlcPdaParams(redeemRequest.Id, htlcProgramIdKey);

        var redeemData = new HtlcSolInstructionDataBuilder().CreateSolRedeemData(redeemRequest);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSolInstructionKeyProvider.CreateSolRedeemAccountKeys(redeemRequest, pdaParams),
            Data = redeemData
        });

        return builder;
    }

    public static TransactionBuilder SetSolRefundTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HtlcSolRefundRequest htlcRefundRequest)
    {
        var pdaParams = GetSolHtlcPdaParams(htlcRefundRequest.Id, htlcProgramIdKey);

        var refundData = new HtlcSolInstructionDataBuilder().CreateSolRefundData(htlcRefundRequest);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSolInstructionKeyProvider.SetSolRefundAccountKeys(htlcRefundRequest, pdaParams),
            Data = refundData
        });

        return builder;
    }

    private static HtlcSolPdaResponse GetSolHtlcPdaParams(
        byte[] Id,
        PublicKey htlcProgramIdKey)
    {
        var htlc = PublicKey.TryFindProgramAddress(
           new List<byte[]>()
               {
                    Id
               },
           htlcProgramIdKey,
           out PublicKey htlcPubKey,
           out _);

        return new()
        {
            HtlcPublicKey = htlcPubKey
        };
    }
}
