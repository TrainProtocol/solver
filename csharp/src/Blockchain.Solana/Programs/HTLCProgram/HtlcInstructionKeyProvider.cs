using Solnet.Programs;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Helpers;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram;

public static class HtlcInstructionKeyProvider
{
    public static List<AccountMeta> CreateLockAccountKeys(
        HTLCLockRequest htlcLockRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcLockRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcTokenAccount, isSigner: false),
            AccountMeta.ReadOnly(publicKey: htlcLockRequest.SourceTokenPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcLockRequest.SignerPublicKey, htlcLockRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.ReadOnly(publicKey: TokenProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static IList<AccountMeta> CreateLockRewardAccountKeys(HTLCLockRequest htlcLockRequest, HTLCPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcLockRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcTokenAccount, isSigner: false),
            AccountMeta.ReadOnly(publicKey: htlcLockRequest.SourceTokenPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcLockRequest.SignerPublicKey, htlcLockRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: TokenProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> SetRefundAccountKeys(
        HTLCRefundRequest refundRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: refundRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcTokenAccount, isSigner: false),
            AccountMeta.Writable(publicKey: refundRequest.ReceiverPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: refundRequest.SourceTokenPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(refundRequest.ReceiverPublicKey, refundRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: TokenProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> CreateRedeemAccountKeys(
       HTLCRedeemRequest htlcRedeemRequest,
       HTLCPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcRedeemRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcRedeemRequest.SenderPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: htlcRedeemRequest.ReceiverPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: htlcRedeemRequest.SourceTokenPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcTokenAccount, isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcRedeemRequest.SenderPublicKey, htlcRedeemRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcRedeemRequest.ReceiverPublicKey, htlcRedeemRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcRedeemRequest.ReceiverPublicKey, htlcRedeemRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: TokenProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: AssociatedTokenAccountProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> CreateGetDetailsAccountKeys(PublicKey htlcPubKey)
    {
        var keys = new List<AccountMeta>
        {
            AccountMeta.ReadOnly(publicKey: htlcPubKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> CreateAddLockSigAccountKeys(
        HTLCAddlocksigRequest hTLCAddlocksigRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>
        {
            AccountMeta.Writable(publicKey: hTLCAddlocksigRequest.SenderPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: new PublicKey(SolanaConstants.SysVarInstructionAddress), isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false),
        };
        return keys;
    }
}
