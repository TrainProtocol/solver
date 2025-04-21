using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Buffers.Binary;

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
}
