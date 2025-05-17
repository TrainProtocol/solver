using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Train.Solver.Blockchain.Solana.Models;
using Solnet.Wallet.Utilities;

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
        const ushort ixIdx = ushort.MaxValue;

        const int prefix = 2;   
        const int offsLen = 14; 

        var pkOffset = (ushort)(prefix + offsLen);      
        var sigOffset = (ushort)(pkOffset + 32);        
        var msgOffset = (ushort)(sigOffset + 64);       
        var msgSize = (ushort)message.Length;

        var data = new byte[prefix + offsLen + 32 + 64 + message.Length];
        data[0] = 1;                                    
        data[1] = 0;                                     

        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2), sigOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), ixIdx);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), pkOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(8), ixIdx);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(10), msgOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(12), msgSize);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(14), ixIdx);

        signerPublicKey.KeyBytes.CopyTo(data.AsSpan(pkOffset));
        signature.CopyTo(data.AsSpan(sigOffset));
        message.CopyTo(data.AsSpan(msgOffset));

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

        var finalMessageHex = Convert.ToHexString(finalMessage).ToLower();
        var messageBytes = Encoding.UTF8.GetBytes(finalMessageHex);

        return messageBytes;
    }
}
