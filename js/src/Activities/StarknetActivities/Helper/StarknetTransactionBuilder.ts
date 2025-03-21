import { utils } from "ethers";
import { cairo, Call, shortString, byteArray } from "starknet";
import { ContractType } from "../../../Data/Entities/Contracts";
import { Networks } from "../../../Data/Entities/Networks";
import { decodeJson } from "../../../Extensions/StringExtensions";
import { ApprovePrepareRequest } from "../../../lib/Model/TransactionBuilderModels/ApprovePrepareRequest";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { TransferPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/TransferPrepareRequest";
import { TransferBuilderResponse } from "../../../lib/Model/TransactionBuilderModels/TransferBuilderResponse";


export class StarknetTransactionBuilder {
    public static CreateRefundCallData(network: Networks, args: string): TransferBuilderResponse {

        const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);

        const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

        const token = network.tokens.find(t => t.asset === refundRequest.Asset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.Asset}`);
        }

        const nativeToken = network.tokens.find(t => t.isNative === true);

        if (!nativeToken) {
            throw new Error(`Native token not found for network ${network.name}`);
        }

        const callData = [cairo.uint256(refundRequest.Id)];

        const methodCall: Call = {
            contractAddress: htlcContractAddress.address,
            entrypoint: "refund",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: nativeToken.asset,
            CallDataAsset: token.asset,
            CallDataAmountInWei: "0",
            CallDataAmount: 0,
            ToAddress: htlcContractAddress.address,
        };
    }

    public static CreateRedeemCallData(network: Networks, args: string): TransferBuilderResponse {

        const redeemRequest = decodeJson<HTLCRedeemTransactionPrepareRequest>(args);

        const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

        const token = network.tokens.find(t => t.asset === redeemRequest.Asset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${redeemRequest.Asset}`);
        }

        const nativeToken = network.tokens.find(t => t.isNative === true);

        if (!nativeToken) {
            throw new Error(`Native token not found for network ${network.name}`);
        }

        const callData = [
            cairo.uint256(redeemRequest.Id),
            cairo.uint256(redeemRequest.Secret)
        ];

        const methodCall: Call = {
            contractAddress: htlcContractAddress.address,
            entrypoint: "redeem",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: nativeToken.asset,
            CallDataAsset: token.asset,
            CallDataAmountInWei: "0",
            CallDataAmount: 0,
            ToAddress: htlcContractAddress.address,
        };
    }

    public static CreateLockCallData(network: Networks, args: string): TransferBuilderResponse {

        const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);

        const token = network.tokens.find(t => t.asset === lockRequest.SourceAsset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.SourceAsset}`)
        };

        const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

        const callData = [
            cairo.uint256(lockRequest.Id),
            cairo.uint256(lockRequest.Hashlock),
            cairo.uint256(Number(utils.parseUnits(lockRequest.Reward.toString(), token.decimals))),
            cairo.uint256(lockRequest.RewardTimelock),
            cairo.uint256(lockRequest.Timelock),
            lockRequest.Receiver,
            shortString.encodeShortString(lockRequest.SourceAsset),
            shortString.encodeShortString(lockRequest.DestinationNetwork),
            byteArray.byteArrayFromString(lockRequest.DestinationAddress),
            shortString.encodeShortString(lockRequest.DestinationAsset),
            cairo.uint256(Number(utils.parseUnits(lockRequest.Amount.toString(), token.decimals))),
            token.tokenContract
        ];

        const methodCall: Call = {
            contractAddress: htlcContractAddress.address,
            entrypoint: "lock",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: lockRequest.SourceAsset,
            CallDataAsset: lockRequest.SourceAsset,
            CallDataAmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
            CallDataAmount: lockRequest.Amount + lockRequest.Reward,
            ToAddress: htlcContractAddress.address,
        };
    }

    public static CreateAddLockSigCallData(network: Networks, args: string): TransferBuilderResponse {

        const addLockSigRequest = decodeJson<HTLCAddLockSigTransactionPrepareRequest>(args);

        const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

        const token = network.tokens.find(t => t.asset === addLockSigRequest.Asset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${addLockSigRequest.Asset}`);
        }

        const nativeToken = network.tokens.find(t => t.isNative === true);

        if (!nativeToken) {
            throw new Error(`Native token not found for network ${network.name}`);
        }

        const callData = [
            cairo.uint256(addLockSigRequest.Id),
            cairo.uint256(addLockSigRequest.Hashlock),
            cairo.uint256(addLockSigRequest.Timelock),
            addLockSigRequest.SignatureArray
        ];
        const methodCall: Call = {
            contractAddress: htlcContractAddress.address,
            entrypoint: "addLockSig",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: nativeToken.asset,
            CallDataAsset: token.asset,
            CallDataAmountInWei: '0',
            CallDataAmount: 0,
            ToAddress: htlcContractAddress.address,
        };
    }

    public static CreateApproveCallData(network: Networks, args: string): TransferBuilderResponse {

        const approveRequest = decodeJson<ApprovePrepareRequest>(args);
        const token = network.tokens.find(t => t.asset === approveRequest.Asset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${approveRequest.Asset}`);
        }

        const callData = [
            approveRequest.SpenderAddress,
            cairo.uint256(Number(utils.parseUnits(approveRequest.Amount.toString(), token.decimals)))
        ];

        const methodCall: Call = {
            contractAddress: token.tokenContract,
            entrypoint: "approve",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: token.asset,
            CallDataAsset: token.asset,
            CallDataAmountInWei: '0',
            CallDataAmount: 0,
            ToAddress: token.tokenContract,
        };
    }

    public static CreateTransferCallData(network: Networks, args: string): TransferBuilderResponse {

        const transferRequest = decodeJson<TransferPrepareRequest>(args);
        const token = network.tokens.find(t => t.asset === transferRequest.Asset);

        if (!token) {
            throw new Error(`Token not found for network ${network.name} and asset ${transferRequest.Asset}`);
        }

        const callData = [
            transferRequest.ToAddress,
            cairo.uint256(Number(utils.parseUnits(transferRequest.Amount.toString(), token.decimals)))
        ];

        const methodCall: Call = {
            contractAddress: token.tokenContract,
            entrypoint: "transfer",
            calldata: callData
        };

        return {
            Data: JSON.stringify(methodCall),
            Amount: 0,
            AmountInWei: "0",
            Asset: token.asset,
            CallDataAsset: token.asset,
            CallDataAmountInWei: Number(utils.parseUnits(transferRequest.Amount.toString(), token.decimals)).toString(),
            CallDataAmount: transferRequest.Amount,
            ToAddress: token.tokenContract,
        };
    }
}
