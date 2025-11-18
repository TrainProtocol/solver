using System.Text;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Programs;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HtlcSplProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram;

public static class HTLCSplProgram
{
    public static TransactionBuilder SetSplLockTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSplLockRequest htlcLockRequest)
    {
        var htlcPdaParams = GetSplHtlcPdaParams(htlcLockRequest.Id, htlcProgramIdKey);

        var lockData = new HtlcSplInstructionDataBuilder().CreateSplLockData(htlcLockRequest);
        var lockRewardData = new HtlcSplInstructionDataBuilder().CreateLockRewardData(htlcLockRequest);

        // Add Lock instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.CreateSplLockAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = lockData

        });
        // Add Reward instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.CreateLockRewardAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = lockRewardData
        });

        return builder;
    }

    public static TransactionBuilder SetSplRedeemTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSplRedeemRequest redeemRequest)
    {
        var pdaParams = GetSplHtlcPdaParams(redeemRequest.Id, htlcProgramIdKey);

        var redeemData = new HtlcSplInstructionDataBuilder().CreateRedeemData(redeemRequest, pdaParams);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.CreateRedeemAccountKeys(redeemRequest, pdaParams),
            Data = redeemData
        });

        return builder;
    }
    
    public static TransactionBuilder SetSplRefundTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HtlcSplRefundRequest htlcRefundRequest)
    {
        var pdaParams = GetSplHtlcPdaParams(htlcRefundRequest.Id, htlcProgramIdKey);

        var refundData = new HtlcSplInstructionDataBuilder().CreateSplRefundData(htlcRefundRequest, pdaParams);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.SetSplRefundAccountKeys(htlcRefundRequest, pdaParams),
            Data = refundData
        });

        return builder;
    }

    public static TransactionBuilder SetAddLockSigInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HtlcAddlocksigRequest htlcAddlocksigRequest)
    {
        var pdaParams = GetSplHtlcPdaParams(htlcAddlocksigRequest.AddLockSigMessageRequest.Id, htlcProgramIdKey);
        var message = Ed25519Program.CreateAddLockSigMessage(htlcAddlocksigRequest.AddLockSigMessageRequest);
        var addLockSigData = new HtlcSplInstructionDataBuilder().CreateAddLockSigData(htlcAddlocksigRequest, pdaParams);

        builder.CreateEd25519Instruction(
            htlcAddlocksigRequest.AddLockSigMessageRequest.SignerPublicKey,
            message,
            htlcAddlocksigRequest.Signature);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.CreateAddLockSigAccountKeys(htlcAddlocksigRequest, pdaParams),
            Data = addLockSigData
        });

        return builder;
    }

    public static TransactionBuilder SetGetDetailsInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        byte[] id)
    {
        var pdaParams = GetSplHtlcPdaParams(id, htlcProgramIdKey);

        var getDetailsData = new HtlcSplInstructionDataBuilder().CreateGetDetailsData(pdaParams, id);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcSplInstructionKeyProvider.CreateGetDetailsAccountKeys(pdaParams.HtlcPublicKey),
            Data = getDetailsData
        });

        return builder;
    }

    private static HTLCSplPdaResponse GetSplHtlcPdaParams(
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
