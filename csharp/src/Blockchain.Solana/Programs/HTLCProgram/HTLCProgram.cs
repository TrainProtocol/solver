using System.Text;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram;

public static class HTLCProgram
{
    public static TransactionBuilder SetLockTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCLockRequest htlcLockRequest)
    {
        var htlcPdaParams = GetHtlcPdaParams(htlcLockRequest.Id, htlcProgramIdKey);

        var lockData = new HtlcInstructionDataBuilder().CreateLockData(htlcLockRequest, htlcPdaParams);
        var lockRewardData = new HtlcInstructionDataBuilder().CreateLockRewardData(htlcLockRequest, htlcPdaParams);

        // Add Lock instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateLockAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = lockData

        });
        // Add Reward instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateLockRewardAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = lockRewardData
        });

        return builder;
    }

    public static TransactionBuilder SetRedeemTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCRedeemRequest redeemRequest)
    {
        var pdaParams = GetHtlcPdaParams(redeemRequest.Id, htlcProgramIdKey);

        var redeemData = new HtlcInstructionDataBuilder().CreateRedeemData(redeemRequest, pdaParams);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateRedeemAccountKeys(redeemRequest, pdaParams),
            Data = redeemData
        });

        return builder;
    }

    public static TransactionBuilder SetRefundTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCRefundRequest htlcRefundRequest)
    {
        var pdaParams = GetHtlcPdaParams(htlcRefundRequest.Id, htlcProgramIdKey);

        var refundData = new HtlcInstructionDataBuilder().CreateRefundData(htlcRefundRequest, pdaParams);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.SetRefundAccountKeys(htlcRefundRequest, pdaParams),
            Data = refundData
        });

        return builder;
    }

    public static TransactionBuilder SetAddLockSigInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCAddlocksigRequest htlcAddlocksigRequest)
    {
        var pdaParams = GetHtlcPdaParams(htlcAddlocksigRequest.Id, htlcProgramIdKey);
        var addLockSigData = new HtlcInstructionDataBuilder().CreateAddLockSigData(htlcAddlocksigRequest, pdaParams);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateAddLockSigAccountKeys(htlcAddlocksigRequest, pdaParams),
            Data = addLockSigData
        });

        builder.CreateEd25519Instruction(
            htlcAddlocksigRequest.SignerPublicKey,
            htlcAddlocksigRequest.Message,
            htlcAddlocksigRequest.Signature);

        return builder;
    }

    public static TransactionBuilder SetGetDetailsInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        byte[] id)
    {
        var pdaParams = GetHtlcPdaParams(id, htlcProgramIdKey);

        var getDetailsData = new HtlcInstructionDataBuilder().CreateGetDetailsData(pdaParams, id);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateGetDetailsAccountKeys(pdaParams.HtlcPublicKey),
            Data = getDetailsData
        });

        return builder;
    }

    private static HTLCPdaResponse GetHtlcPdaParams(
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
           out byte htlcBump);

        var htlcAccount = PublicKey.TryFindProgramAddress(
           new List<byte[]>()
               {
                    Encoding.UTF8.GetBytes("htlc_token_account"),
                    Id
               },
           htlcProgramIdKey,
           out PublicKey htlcTokenAccount,
           out byte htlcAccountBump);

        return new()
        {
            HtlcPublicKey = htlcPubKey,
            HtlcTokenAccount = htlcTokenAccount,
            HtlcBump = htlcBump
        };
    }
}
