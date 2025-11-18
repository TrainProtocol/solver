using Solnet.Programs;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram;

public static class HtlcSolInstructionKeyProvider
{
    public static List<AccountMeta> CreateSolLockAccountKeys(
        HTLCSolLockRequest htlcLockRequest,
        HtlcSolPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcLockRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static IList<AccountMeta> CreateSolLockRewardAccountKeys(HTLCSolLockRequest htlcLockRequest, HtlcSolPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcLockRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> SetSolRefundAccountKeys(
        HtlcSplRefundRequest refundRequest,
        HTLCSplPdaResponse htlcPdaResponse)
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

    public static List<AccountMeta> CreateSolRedeemAccountKeys(
       HTLCSolRedeemRequest htlcRedeemRequest,
       HtlcSolPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: htlcRedeemRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcRedeemRequest.SenderPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcRedeemRequest.ReceiverPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    public static List<AccountMeta> SetSolRefundAccountKeys(
        HtlcSolRefundRequest refundRequest,
        HtlcSolPdaResponse htlcPdaResponse)
    {
        var keys = new List<AccountMeta>()
        {
            AccountMeta.Writable(publicKey: refundRequest.SignerPublicKey, isSigner: true),
            AccountMeta.Writable(publicKey: htlcPdaResponse.HtlcPublicKey, isSigner: false),
            AccountMeta.Writable(publicKey: refundRequest.ReceiverPublicKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
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
}
