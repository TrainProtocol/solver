using System.Text;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Programs;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram;

public static class HTLCProgram
{
    public static TransactionBuilder SetSplLockTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSplLockRequest htlcLockRequest)
    {
        var htlcPdaParams = GetSPLHtlcPdaParams(htlcLockRequest.Id, htlcProgramIdKey);

        var lockData = new HtlcInstructionDataBuilder().CreateLockData(htlcLockRequest);
        var lockRewardData = new HtlcInstructionDataBuilder().CreateLockRewardData(htlcLockRequest);

        // Add Lock instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateSplLockAccountKeys(htlcLockRequest, htlcPdaParams),
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

    public static TransactionBuilder SetSolLockTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCSolLockRequest htlcLockRequest)
    {
        var pdaParams = GetSolHtlcParams(htlcLockRequest.Id, htlcProgramIdKey);

        var lockData = new HtlcInstructionDataBuilder().CreateLockData(htlcLockRequest);
        var lockRewardData = new HtlcInstructionDataBuilder().CreateLockRewardData(htlcLockRequest);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateSolLockAccountKeys(htlcLockRequest, pdaParams),
            Data = lockData
        });

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateSolLockRewardAccountKeys(htlcLockRequest, pdaParams),
            Data = lockRewardData
        });
        return builder;
    }

    public static TransactionBuilder SetRedeemTransactionInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        HTLCRedeemRequest redeemRequest)
    {
        var pdaParams = GetSPLHtlcPdaParams(redeemRequest.Id, htlcProgramIdKey);

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
        var pdaParams = GetSPLHtlcPdaParams(htlcRefundRequest.Id, htlcProgramIdKey);

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
        var pdaParams = GetSPLHtlcPdaParams(htlcAddlocksigRequest.AddLockSigMessageRequest.Id, htlcProgramIdKey);
        var message = Ed25519Program.CreateAddLockSigMessage(htlcAddlocksigRequest.AddLockSigMessageRequest);
        var addLockSigData = new HtlcInstructionDataBuilder().CreateAddLockSigData(htlcAddlocksigRequest, pdaParams);

        builder.CreateEd25519Instruction(
            htlcAddlocksigRequest.AddLockSigMessageRequest.SignerPublicKey,
            message,
            htlcAddlocksigRequest.Signature);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateAddLockSigAccountKeys(htlcAddlocksigRequest, pdaParams),
            Data = addLockSigData
        });

        return builder;
    }

    public static TransactionBuilder SetGetDetailsInstruction(
        this TransactionBuilder builder,
        PublicKey htlcProgramIdKey,
        byte[] id)
    {
        var pdaParams = GetSPLHtlcPdaParams(id, htlcProgramIdKey);

        var getDetailsData = new HtlcInstructionDataBuilder().CreateGetDetailsData(pdaParams, id);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = HtlcInstructionKeyProvider.CreateGetDetailsAccountKeys(pdaParams.HtlcPublicKey),
            Data = getDetailsData
        });

        return builder;
    }

    private static HTLCSplPdaResponse GetSPLHtlcPdaParams(
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

    private static HTLCSolPdaResponse GetSolHtlcParams(
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
