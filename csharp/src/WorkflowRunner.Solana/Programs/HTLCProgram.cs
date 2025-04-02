using System.Numerics;
using System.Text;
using Solnet.Programs;
using Solnet.Programs.Utilities;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Programs.Models;
using FieldEncoder = Train.Solver.Blockchain.Solana.Helpers.FieldEncoder;

namespace Train.Solver.Blockchain.Solana.Programs;

public static class HTLCProgram
{
    public static TransactionBuilder SetLockTransactionInstruction(
        this TransactionBuilder builder,
        byte[] lockRewardDescriminator,
        byte[] lockDescriminator,
        PublicKey htlcProgramIdKey,
        HTLCLockRequest htlcLockRequest)
    {
        var htlcPdaParams = GetHtlcPdaParams(htlcLockRequest.Id, htlcProgramIdKey);

        // Add Lock instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = SetLockAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = SetLockData(lockDescriminator, htlcLockRequest, htlcPdaParams)
        });

        // Add Reward instruction
        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = SetLockRewardAccountKeys(htlcLockRequest, htlcPdaParams),
            Data = SetLockRewardData(lockRewardDescriminator, htlcLockRequest, htlcPdaParams)
        });

        return builder;
    }

    public static TransactionBuilder SetRedeemTransactionInstruction(
        this TransactionBuilder builder,
        byte[] redeemDescriminator,
        PublicKey htlcProgramIdKey,
        HTLCRedeemRequest redeemRequest)
    {
        var pdaParams = GetHtlcPdaParams(redeemRequest.Id, htlcProgramIdKey);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = SetRedeemAccountKeys(redeemRequest, pdaParams),
            Data = SetRedeemData(redeemDescriminator, redeemRequest, pdaParams)
        });

        return builder;
    }

    public static TransactionBuilder SetRefundTransactionInstruction(
        this TransactionBuilder builder,
        byte[] refundDescriminator,
        PublicKey htlcProgramIdKey,
        HTLCRefundRequest htlcRefundRequest)
    {
        var pdaParams = GetHtlcPdaParams(htlcRefundRequest.Id, htlcProgramIdKey);

        builder.AddInstruction(new()
        {
            ProgramId = htlcProgramIdKey,
            Keys = SetRefundAccountKeys(htlcRefundRequest, pdaParams),
            Data = SetRefundData(refundDescriminator, htlcRefundRequest, pdaParams)
        });

        return builder;
    }

    private static List<AccountMeta> SetLockAccountKeys(
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

    private static byte[] SetLockData(
        byte[] lockDescriminator,
        HTLCLockRequest lockRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var destinationAssetData = Encoding.UTF8.GetBytes(lockRequest.DestinationAsset);
        var sourceAddressData = Encoding.UTF8.GetBytes(lockRequest.SourceAddress);
        var destinationNetworkData = Encoding.UTF8.GetBytes(lockRequest.DestinationNetwork);
        var sourceAssetData = Encoding.UTF8.GetBytes(lockRequest.SourceAsset);

        var fields = new List<FieldEncoder.Field>
        {
            new FieldEncoder.Field
            {
                Span = lockRequest.Id.Length,
                Property = "id",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = lockRequest.Hashlock.Length,
                Property = "hashlock",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 8,
                Property = "timelock",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var bigInt = (BigInteger)value;
                    buffer.WriteBigInt(bigInt, offset, 8, isUnsigned: true, isBigEndian: false);
                }
            },
            new FieldEncoder.Field
            {
                Span =  destinationNetworkData.Length + 4,
                Property = "destinationNetwork",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArrayWithLength(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = sourceAddressData.Length + 4,
                Property = "sourceAddress",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArrayWithLength(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = destinationAssetData.Length + 4,
                Property = "destinationAsset",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArrayWithLength(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = sourceAssetData.Length + 4,
                Property = "sourceAsset",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArrayWithLength(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 32,
                Property = "receiver",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var publicKey = (PublicKey)value;
                    buffer.WritePubKey(publicKey, offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 8,
                Property = "amount",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var bigInt = (BigInteger)value;
                    buffer.WriteBigInt(bigInt, offset, 8, isUnsigned: true, isBigEndian: false);
                }
            },
            new FieldEncoder.Field
            {
                Span = 1,
                Property = "htlcBump",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteValue = (byte)value;
                    buffer.WriteU8(byteValue, offset);
                }
            }
        };

        var src = new Dictionary<string, object>
        {
            { "id", lockRequest.Id},
            { "hashlock", lockRequest.Hashlock},
            { "timelock", lockRequest.Timelock},
            { "destinationNetwork", destinationNetworkData },
            { "sourceAddress", sourceAddressData },
            { "destinationAsset", destinationAssetData },
            { "sourceAsset", sourceAssetData },
            { "receiver", lockRequest.ReceiverPublicKey },
            { "amount", lockRequest.Amount},
            { "htlcBump", htlcPdaResponse.HtlcBump }
        };

        return FieldEncoder.Encode(fields, src, lockDescriminator);
    }

    private static List<AccountMeta> SetRedeemAccountKeys(
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
            AccountMeta.Writable(publicKey: AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(htlcRedeemRequest.SenderPublicKey, htlcRedeemRequest.SourceTokenPublicKey), isSigner: false),
            AccountMeta.ReadOnly(publicKey: SystemProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: TokenProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: AssociatedTokenAccountProgram.ProgramIdKey, isSigner: false),
            AccountMeta.ReadOnly(publicKey: SysVars.RentKey, isSigner: false)
        };

        return keys;
    }

    private static byte[] SetRedeemData(
        byte[] redeemDescriminator,
        HTLCRedeemRequest htlcRedeemRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var fields = new List<FieldEncoder.Field>
        {
            new FieldEncoder.Field
            {
                Span = htlcRedeemRequest.Id.Length,
                Property = "id",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = htlcRedeemRequest.Secret.Length,
                Property = "secret",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 1,
                Property = "htlcBump",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteValue = (byte)value;
                    buffer.WriteU8(byteValue, offset);
                }
            }
        };

        var src = new Dictionary<string, object>
        {
            { "id", htlcRedeemRequest.Id},
            { "secret", htlcRedeemRequest.Secret},
            { "htlcBump", htlcPdaResponse.HtlcBump}
        };

        return FieldEncoder.Encode(fields, src, redeemDescriminator);
    }

    private static List<AccountMeta> SetRefundAccountKeys(
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

    private static byte[] SetRefundData(
        byte[] refundDescriminator,
        HTLCRefundRequest refundRequest,
        HTLCPdaResponse htlcPdaResponse)
    {
        var fields = new List<FieldEncoder.Field>
        {
            new FieldEncoder.Field
            {
                Span = refundRequest.Id.Length,
                Property = "id",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 1,
                Property = "htlcBump",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteValue = (byte)value;
                    buffer.WriteU8(byteValue, offset);
                }
            }
        };

        var src = new Dictionary<string, object>
        {
            { "id",  refundRequest.Id },
            { "htlcBump", htlcPdaResponse.HtlcBump }
        };

        return FieldEncoder.Encode(fields, src, refundDescriminator);
    }


    private static byte[] SetLockRewardData(byte[] lockRewardDescriminator, HTLCLockRequest lockRequest, HTLCPdaResponse htlcPdaResponse)
    {
        var fields = new List<FieldEncoder.Field>
        {
            new FieldEncoder.Field
            {
                Span = lockRequest.Id.Length,
                Property = "id",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteArray = (byte[]) value;
                    FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
                }
            },
            new FieldEncoder.Field
            {
                Span = 8,
                Property = "rewardTimelock",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var bigInt = (BigInteger)value;
                    buffer.WriteBigInt(bigInt, offset, 8, isUnsigned: true, isBigEndian: false);
                }
            },
            new FieldEncoder.Field
            {
                Span = 8,
                Property = "reward",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var bigInt = (BigInteger)value;
                    buffer.WriteBigInt(bigInt, offset, 8, isUnsigned: true, isBigEndian: false);
                }
            },
            new FieldEncoder.Field
            {
                Span = 1,
                Property = "htlcBump",
                EncoderFunc = (value, buffer, offset) =>
                {
                    var byteValue = (byte)value;
                    buffer.WriteU8(byteValue, offset);
                }
            }
        };

        var src = new Dictionary<string, object>
        {
            { "id", lockRequest.Id},
            { "timelock", lockRequest.RewardTimelock},
            { "amount", lockRequest.Reward},
            { "htlcBump", htlcPdaResponse.HtlcBump }
        };

        return FieldEncoder.Encode(fields, src, lockRewardDescriminator);
    }

    private static IList<AccountMeta> SetLockRewardAccountKeys(HTLCLockRequest htlcLockRequest, HTLCPdaResponse htlcPdaResponse)
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
