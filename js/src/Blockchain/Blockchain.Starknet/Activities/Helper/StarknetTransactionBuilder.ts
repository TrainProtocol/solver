import { utils } from "ethers";
import { cairo, Call, shortString, byteArray } from "starknet";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { ApprovePrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/ApprovePrepareRequest";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { TransferPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";

export function CreateRefundCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === refundRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [cairo.uint256(refundRequest.commitId)];

    const methodCall: Call = {
        contractAddress: htlcContractAddress,
        entrypoint: "refund",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: network.nativeToken.symbol,
        callDataAmount: 0,
        callDataAsset: token.symbol,
        toAddress: htlcContractAddress,
    };
}

export function CreateRedeemCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const redeemRequest = decodeJson<HTLCRedeemTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === redeemRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${redeemRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [
        cairo.uint256(redeemRequest.commitId),
        cairo.uint256(redeemRequest.secret)
    ];

    const methodCall: Call = {
        contractAddress: htlcContractAddress,
        entrypoint: "redeem",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: network.nativeToken.symbol,
        callDataAsset: token.symbol,
        callDataAmount: 0,
        toAddress: htlcContractAddress,
    };
}

export function CreateLockCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === lockRequest.sourceAsset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.sourceAsset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [
        cairo.uint256(lockRequest.commitId),
        cairo.uint256(lockRequest.hashlock),
        cairo.uint256(Number(utils.parseUnits(lockRequest.reward.toString(), token.decimals))),
        cairo.uint256(lockRequest.rewardTimelock),
        cairo.uint256(lockRequest.timelock),
        lockRequest.receiver,
        shortString.encodeShortString(lockRequest.sourceAsset),
        shortString.encodeShortString(lockRequest.destinationNetwork),
        byteArray.byteArrayFromString(lockRequest.destinationAddress),
        shortString.encodeShortString(lockRequest.destinationAsset),
        cairo.uint256(Number(utils.parseUnits(lockRequest.amount.toString(), token.decimals))),
        token.contract
    ];

    const methodCall: Call = {
        contractAddress: htlcContractAddress,
        entrypoint: "lock",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: lockRequest.sourceAsset,
        callDataAsset: lockRequest.sourceAsset,
        callDataAmount: lockRequest.amount + lockRequest.reward,
        toAddress: htlcContractAddress,
    };
}

export function CreateAddLockSigCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const addLockSigRequest = decodeJson<HTLCAddLockSigTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === addLockSigRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${addLockSigRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [
        cairo.uint256(addLockSigRequest.commitId),
        cairo.uint256(addLockSigRequest.hashlock),
        cairo.uint256(addLockSigRequest.timelock),
        addLockSigRequest.signatureArray
    ];
    const methodCall: Call = {
        contractAddress: htlcContractAddress,
        entrypoint: "addLockSig",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: network.nativeToken.symbol,
        callDataAsset: token.symbol,
        callDataAmount: 0,
        toAddress: htlcContractAddress,
    };
}

export function CreateApproveCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const approveRequest = decodeJson<ApprovePrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === approveRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${approveRequest.asset}`)
    };

    const spenderAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [
        spenderAddress,
        cairo.uint256(Number(utils.parseUnits(approveRequest.amount.toString(), token.decimals)))
    ];

    const methodCall: Call = {
        contractAddress: token.contract,
        entrypoint: "approve",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: token.symbol,
        callDataAsset: token.symbol,
        callDataAmount: 0,
        toAddress: token.contract,
    };
}

export function CreateTransferCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const transferRequest = decodeJson<TransferPrepareRequest>(args);
    const token = network.tokens.find(t => t.symbol === transferRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${transferRequest.asset}`)
    };

    const callData = [
        transferRequest.toAddress,
        cairo.uint256(Number(utils.parseUnits(transferRequest.amount.toString(), token.decimals)))
    ];

    const methodCall: Call = {
        contractAddress: token.contract,
        entrypoint: "transfer",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: token.symbol,
        callDataAsset: token.symbol,
        callDataAmount: transferRequest.amount,
        toAddress: token.contract,
    };
}

export function CreateCommitCallData(network: DetailedNetworkDto, args: string): PrepareTransactionResponse {

    const commitRequest = decodeJson<HTLCCommitTransactionPrepareRequest>(args);

     const token = network.tokens.find(t => t.symbol === commitRequest.sourceAsset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.sourceAsset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const callData = [
        cairo.uint256(commitRequest.commitId),
        cairo.uint256(Number(utils.parseUnits(commitRequest.amount.toString(), token.decimals))),
        shortString.encodeShortString(commitRequest.destinationChain),
        shortString.encodeShortString(commitRequest.sourceAsset),
        byteArray.byteArrayFromString(commitRequest.destinationAddress),
        shortString.encodeShortString(commitRequest.sourceAsset),
        commitRequest.receiver,
        cairo.uint256(commitRequest.timelock),
        token.contract
    ];

    const methodCall: Call = {
        contractAddress: htlcContractAddress,
        entrypoint: "commit",
        calldata: callData
    };

    return {
        data: JSON.stringify(methodCall),
        amount: 0,
        asset: commitRequest.sourceAsset,
        callDataAsset: commitRequest.sourceAsset,
        callDataAmount: commitRequest.amount,
        toAddress: htlcContractAddress,
    };
}
