import { BigNumber } from "ethers";
import { ApproveTransactionBuilderRequest } from "../../../lib/models/TransactionBuilderModels/ApproveTransactionBuilderRequest";
import { HTLCAddLockSigBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCAddLockSigBuildetRequest";
import { HTLCRedeemTransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCRedeemTransferBuilderRequest";
import { HTLCRefundTransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCRefundTransferBuilderRequest";
import { TransferBuilderResponse } from "../../../lib/models/TransactionBuilderModels/TransferBuilderResponse";
import { Call, CallData, RawArgs, byteArray, cairo, shortString } from "starknet";
import { TransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/TransferBuilderRequest";
import { HTLCLockTransferBuilderRequest } from "../../../lib/models/TransactionBuilderModels/HTLCLockTransferBuilderRequest";
import { FunctionName} from "../../../lib/models/TransactionBuilderModels/TransferBuilderRequestBase";

export async function StarknetTransactionBuilderActivity(
    request: HTLCLockTransferBuilderRequest | HTLCRefundTransferBuilderRequest | HTLCRedeemTransferBuilderRequest | HTLCAddLockSigBuilderRequest | ApproveTransactionBuilderRequest | TransferBuilderRequest): Promise<TransferBuilderResponse> {
    try {
        let response: TransferBuilderResponse = {
            Data: "",
            Amount: 0,
            AmountInWei: "0",
            ToAddress: "",
            CallDataAmount: 0,
            CallDataAmountInWei: "0",
            CallDataAsset: "ETH",
            Asset: "ETH"
        };

        var callData: RawArgs = null;

        switch (request.FunctionName) {
            case FunctionName.Lock:
                {
                    var lockRequest = request as HTLCLockTransferBuilderRequest;
                    callData = CreateLockCallData(lockRequest);
                    response.CallDataAmount = lockRequest.Amount;
                    response.CallDataAmountInWei = lockRequest.AmountInWei;
                    response.ToAddress = lockRequest.ContractAddress;

                    break;
                }
            case FunctionName.Redeem:
                {
                    var redeemRequest = request as HTLCRedeemTransferBuilderRequest;
                    callData = CreateRedeemCallData(redeemRequest);
                    response.ToAddress = redeemRequest.ContractAddress;
                    break;
                }
            case FunctionName.Refund:
                {
                    var refundRequest = request as HTLCRefundTransferBuilderRequest;
                    callData = CreateRefundCallData(refundRequest);
                    response.ToAddress = refundRequest.ContractAddress;
                    break;
                }
            case FunctionName.AddLockSig:
                {
                    var addLockSigRequest = request as HTLCAddLockSigBuilderRequest;

                    callData = CreateAddLockSigCallData(addLockSigRequest);
                    response.ToAddress = addLockSigRequest.ContractAddress;
                    break;
                }
            case FunctionName.Approve:
                {
                    var approveRequest = request as ApproveTransactionBuilderRequest;

                    callData = CallData.compile({
                        spender: approveRequest.Spender,
                        amount: cairo.uint256(BigNumber.from(approveRequest.AmountInWei).toHexString())
                    })
                    break;
                }
            case FunctionName.Transfer:
                {
                    var transferRequest = request as TransferBuilderRequest;

                    callData = CallData.compile({
                        recipient: transferRequest.ToAddress,
                        amount: cairo.uint256(transferRequest.AmountInWei)
                    })
                    break;
                }
            default: {
                throw new Error(`Unknown function name ${request.FunctionName}`);
            }
        }

        const methodCall: Call =
        {
            contractAddress: request.ContractAddress,
            entrypoint: request.FunctionName,
            calldata: callData
        };

        response.Data = JSON.stringify(methodCall)

        return response;
    }
    catch (error) {
        throw error;
    }
}

function CreateRefundCallData(refundRequest: HTLCRefundTransferBuilderRequest) {
    var callData = [
        cairo.uint256(refundRequest.Id),
    ];
    return callData;
}

function CreateRedeemCallData(redeemRequest: HTLCRedeemTransferBuilderRequest) {
    var callData = [
        cairo.uint256(redeemRequest.Id),
        cairo.uint256(redeemRequest.Secret)
    ];
    return callData;
}

function CreateLockCallData(lockRequest: HTLCLockTransferBuilderRequest) {
    var callData = [
        cairo.uint256(lockRequest.Id),
        cairo.uint256(lockRequest.Hashlock),
        cairo.uint256(lockRequest.RewardInWei),
        cairo.uint256(lockRequest.RewardTimelock),
        cairo.uint256(lockRequest.Timelock),
        lockRequest.Receiver,
        shortString.encodeShortString(lockRequest.SourceAsset),
        shortString.encodeShortString(lockRequest.DestinationChain),
        byteArray.byteArrayFromString(lockRequest.DestinationAddress),
        shortString.encodeShortString(lockRequest.DestinationAsset),
        cairo.uint256(lockRequest.AmountInWei),
        lockRequest.TokenContract
    ];

    return callData;
}

function CreateAddLockSigCallData(addLockSigRequest: HTLCAddLockSigBuilderRequest) {
    var callData = [
        cairo.uint256(addLockSigRequest.Id),
        cairo.uint256(addLockSigRequest.Hashlock),
        cairo.uint256(addLockSigRequest.Timelock),
        addLockSigRequest.SignatureArray
    ];
    return callData;
}