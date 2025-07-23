import { utils } from "ethers";
import { cairo, Call, shortString, byteArray } from "starknet";
import { ContractType } from "../../../../Data/Entities/Contracts";
import { Networks } from "../../../../Data/Entities/Networks";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { ApprovePrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/ApprovePrepareRequest";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { TransferPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";

export function CreateRefundCallData(network: Networks, args: string): PrepareTransactionResponse {

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
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: "0",
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export function CreateRedeemCallData(network: Networks, args: string): PrepareTransactionResponse {

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
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: "0",
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export function CreateLockCallData(network: Networks, args: string): PrepareTransactionResponse {

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
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: lockRequest.SourceAsset,
        callDataAsset: lockRequest.SourceAsset,
        CallDataAmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
        callDataAmount: lockRequest.Amount + lockRequest.Reward,
        toAddress: htlcContractAddress.address,
    };
}

export function CreateAddLockSigCallData(network: Networks, args: string): PrepareTransactionResponse {

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
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: '0',
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export function CreateApproveCallData(network: Networks, args: string): PrepareTransactionResponse {

    const approveRequest = decodeJson<ApprovePrepareRequest>(args);
    const token = network.tokens.find(t => t.asset === approveRequest.Asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${approveRequest.Asset}`);
    }

    const spenderAddress = !token.tokenContract
        ? network.contracts.find(c => c.type === ContractType.HTLCNativeContractAddress)!.address
        : network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)!.address;

    const callData = [
        spenderAddress,
        cairo.uint256(Number(utils.parseUnits(approveRequest.Amount.toString(), token.decimals)))
    ];

    const methodCall: Call = {
        contractAddress: token.tokenContract,
        entrypoint: "approve",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: token.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: '0',
        callDataAmount: 0,
        toAddress: token.tokenContract,
    };
}

export function CreateTransferCallData(network: Networks, args: string): PrepareTransactionResponse {

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
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: token.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: Number(utils.parseUnits(transferRequest.Amount.toString(), token.decimals)).toString(),
        callDataAmount: transferRequest.Amount,
        toAddress: token.tokenContract,
    };
}

export function CreateCommitCallData(network: Networks, args: string): PrepareTransactionResponse {

    const commitRequest = decodeJson<HTLCCommitTransactionPrepareRequest>(args);

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

    const token = network.tokens.find(t => t.asset === commitRequest.SourceAsset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.SourceAsset}`);
    }

    const callData = [
        cairo.uint256(commitRequest.Id),
        cairo.uint256(Number(utils.parseUnits(commitRequest.Amount.toString(), token.decimals))),
        shortString.encodeShortString(commitRequest.DestinationChain),
        shortString.encodeShortString(commitRequest.SourceAsset),
        byteArray.byteArrayFromString(commitRequest.DestinationAddress),
        shortString.encodeShortString(commitRequest.SourceAsset),
        commitRequest.Receiver,
        cairo.uint256(commitRequest.Timelock),
        token.tokenContract
    ];

    const methodCall: Call = {
        contractAddress: htlcContractAddress.address,
        entrypoint: "commit",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        AmountInWei: "0",
        asset: commitRequest.SourceAsset,
        callDataAsset: commitRequest.SourceAsset,
        CallDataAmountInWei: utils.parseUnits((commitRequest.Amount).toString(), token.decimals).toString(),
        callDataAmount: commitRequest.Amount,
        toAddress: htlcContractAddress.address,
    };
}
