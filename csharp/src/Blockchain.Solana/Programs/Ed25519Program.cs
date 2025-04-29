using Nethereum.Hex.HexConvertors.Extensions;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Train.Solver.Blockchain.Solana.Models;

namespace Train.Solver.Blockchain.Solana.Programs;

public static class Ed25519Program
{
    public static readonly PublicKey ProgramIdKey = new("Ed25519SigVerify111111111111111111111111111");

    public static TransactionBuilder CreateEd25519Instruction(
        this TransactionBuilder builder,
        PublicKey signerPublicKey,
        byte[] message,
        byte[] signature)
    {
        const ushort CURRENT_IX = ushort.MaxValue;

        var offsets = new byte[14];
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(0), (ushort)(1 + offsets.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(2), CURRENT_IX);
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(4), (ushort)(1 + offsets.Length + 64));
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(6), CURRENT_IX);
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(8), (ushort)(1 + offsets.Length + 64 + 32));
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(10), (ushort)message.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(offsets.AsSpan(12), CURRENT_IX);

        var data = new byte[1 + offsets.Length + 64 + 32 + message.Length];
        data[0] = 1;
        offsets.CopyTo(data.AsSpan(1));
        signature.CopyTo(data.AsSpan(1 + offsets.Length));
        signerPublicKey.KeyBytes.CopyTo(data.AsSpan(1 + offsets.Length + 64));
        message.CopyTo(data.AsSpan(1 + offsets.Length + 64 + 32));

        builder.AddInstruction(new()
        {
            ProgramId = ProgramIdKey,
            Keys = new List<AccountMeta>(),
            Data = data
        });

        return builder;
    }

    public static byte[] CreateAddLockSigMessage(SolanaAddLockSigMessageRequest messageRequest)
    {
        var timelockLe = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(timelockLe, (ulong)messageRequest.Timelock);

        byte[] msg;
        using (var sha = SHA256.Create())
        {
            sha.TransformBlock(messageRequest.Id, 0, messageRequest.Id.Length, null, 0);
            sha.TransformBlock(messageRequest.Hashlock, 0, messageRequest.Hashlock.Length, null, 0);
            sha.TransformFinalBlock(timelockLe, 0, timelockLe.Length);
            msg = sha.Hash!;
        }

        var signingDomain = new byte[] { 0xFF }
            .Concat(Encoding.ASCII.GetBytes("solana offchain"))
            .ToArray();

        var headerVersion = new byte[] { 0x00 };
        var applicationDomain = new byte[32];
        Encoding.ASCII.GetBytes("Train", applicationDomain);
        var messageFormat = new byte[] { 0x00 };
        var signerCount = new byte[] { 0x01 };

        var signerPublicKeyBytes = messageRequest.SignerPublicKey.KeyBytes;

        var messageLengthLe = BitConverter.GetBytes((ushort)msg.Length);

        var parts = new List<byte[]>
        {
            signingDomain,
            headerVersion,
            applicationDomain,
            messageFormat,
            signerCount,
            signerPublicKeyBytes,
            messageLengthLe,
            msg
        };

        var totalLength = parts.Sum(p => p.Length);

        var finalMessage = new byte[totalLength];
        var offset = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, finalMessage, offset, p.Length);
            offset += p.Length;
        }

        var finalMessageHex = finalMessage.ToHex();
        var finalMessageBytes = finalMessageHex.HexToByteArray();

        return finalMessageBytes;
    }
}
