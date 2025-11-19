using Solnet.Programs.Utilities;
using Solnet.Wallet;
using System.Numerics;
using System.Text;
using Train.Solver.Blockchain.Solana.Helpers;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram;

public class HtlcSolInstructionDataBuilder
{
    private List<FieldEncoder.Field> Fields { get; set; } = new();

    private void SetFieldData(string property, int span, Action<object, byte[], int> encoderFunc)
    {
        Fields.Add(new FieldEncoder.Field
        {
            Property = property,
            Span = span,
            EncoderFunc = encoderFunc
        });
    }

    public byte[] CreateSolLockData(
        HTLCSolLockRequest lockRequest)
    {
        var destinationAssetData = Encoding.UTF8.GetBytes(lockRequest.DestinationAsset);
        var destionationAddressData = Encoding.UTF8.GetBytes(lockRequest.DestinationAddress);
        var destinationNetworkData = Encoding.UTF8.GetBytes(lockRequest.DestinationNetwork);
        var sourceAssetData = Encoding.UTF8.GetBytes(lockRequest.SourceAsset);

        SetFieldData("id", lockRequest.Id.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));
        SetFieldData("hashlock", lockRequest.Hashlock.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));
        SetFieldData("timelock", 8, (v, buf, off) => buf.WriteBigInt((BigInteger)v, off, 8, isUnsigned: true, isBigEndian: false));
        SetFieldData("amount", 8, (v, buf, off) => buf.WriteBigInt((BigInteger)v, off, 8, isUnsigned: true, isBigEndian: false));
        SetFieldData("destinationNetwork", destinationNetworkData.Length + 4, (v, buf, off) => FieldEncoder.EncodeByteArrayWithLength((byte[])v, buf, ref off));
        SetFieldData("destinationAddress", destionationAddressData.Length + 4, (v, buf, off) => FieldEncoder.EncodeByteArrayWithLength((byte[])v, buf, ref off));
        SetFieldData("destinationAsset", destinationAssetData.Length + 4, (v, buf, off) => FieldEncoder.EncodeByteArrayWithLength((byte[])v, buf, ref off));
        SetFieldData("sourceAsset", sourceAssetData.Length + 4, (v, buf, off) => FieldEncoder.EncodeByteArrayWithLength((byte[])v, buf, ref off));
        SetFieldData("receiver", 32, (v, buf, off) => buf.WritePubKey((PublicKey)v, off));

        var instructionExecutionOrder = new Dictionary<string, object>
        {
            { "id", lockRequest.Id},
            { "hashlock", lockRequest.Hashlock},
            { "timelock", lockRequest.Timelock},
            { "amount", lockRequest.Amount},
            { "destinationNetwork", destinationNetworkData },
            { "destinationAddress", destionationAddressData },
            { "destinationAsset", destinationAssetData },
            { "sourceAsset", sourceAssetData },
            { "receiver", lockRequest.ReceiverPublicKey },
        };

        return BuildInstructionData(
            instructionExecutionOrder,
            FieldEncoder.Sighash(SolanaConstants.LockSighash));
    }

    public byte[] CreateLockRewardData(
        HTLCSolLockRequest lockRequest)
    {
        SetFieldData("id", lockRequest.Id.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));
        SetFieldData("rewardTimelock", 8, (v, buf, off) => buf.WriteBigInt((BigInteger)v, off, 8, isUnsigned: true, isBigEndian: false));
        SetFieldData("reward", 8, (v, buf, off) => buf.WriteBigInt((BigInteger)v, off, 8, isUnsigned: true, isBigEndian: false));

        var instructionExecutionOrder = new Dictionary<string, object>
        {
            { "id", lockRequest.Id},
            { "rewardTimelock", lockRequest.RewardTimelock},
            { "reward", lockRequest.Reward},
        };

        return BuildInstructionData(
            instructionExecutionOrder,
            FieldEncoder.Sighash(SolanaConstants.LockRewardSighash));
    }

    public byte[] CreateSolRedeemData(HTLCSolRedeemRequest redeemRequest)
    {
        SetFieldData("id", redeemRequest.Id.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));
        SetFieldData("secret", redeemRequest.Secret.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));

        var instructionExecutionOrder = new Dictionary<string, object>
        {
            { "id", redeemRequest.Id},
            { "secret", redeemRequest.Secret},
        };

        return BuildInstructionData(
            instructionExecutionOrder,
            FieldEncoder.Sighash(SolanaConstants.RedeemSighash));
    }

    public byte[] CreateRefundData(
        HtlcSplRefundRequest refundRequest,
        HTLCSplPdaResponse htlcPdaResponse)
    {
        SetFieldData("id", refundRequest.Id.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));
        SetFieldData("htlcBump", 1, (v, buf, off) => buf.WriteU8((byte)v, off));

        var instructionExecutionOrder = new Dictionary<string, object>
        {
            { "id",  refundRequest.Id },
            { "htlcBump", htlcPdaResponse.HtlcBump }
        };

        return BuildInstructionData(
            instructionExecutionOrder,
            FieldEncoder.Sighash(SolanaConstants.RefundSighash));
    }

    public byte[] CreateSolRefundData(HtlcSolRefundRequest refundRequest)
    {
        SetFieldData("id", refundRequest.Id.Length, (v, buf, off) => FieldEncoder.EncodeByteArray((byte[])v, buf, ref off));

        var instructionExecutionOrder = new Dictionary<string, object>
            {
                { "id",  refundRequest.Id },
            };

        return BuildInstructionData(
            instructionExecutionOrder,
            FieldEncoder.Sighash(SolanaConstants.RefundSighash));
    }

    private byte[] BuildInstructionData(Dictionary<string, object> instructionExecutionOrder, byte[] descriminator)
    {
        return FieldEncoder.Encode(Fields, instructionExecutionOrder, descriminator);
    }
}
