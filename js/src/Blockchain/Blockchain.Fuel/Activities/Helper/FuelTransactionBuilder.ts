import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { Address, AssetId, bn, Contract, DateTime, Provider, ScriptTransactionRequest, Wallet } from "fuels";
import abi from '../ABIs/train.json';
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { TransferPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest";

export async function createRefundCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

    const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === refundRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const provider = new Provider(network.nodes[0].url);
    const contractInstance = new Contract(htlcContractAddress, abi, provider);

    const callConfig = contractInstance.functions
        .refund(refundRequest.commitId)
        .txParams({
            maxFee: bn(1000000),
        });

    const txRequest = await callConfig.getTransactionRequest();

    return {
        data: JSON.stringify(txRequest),
        amount: "0",
        asset: network.nativeToken.symbol,
        callDataAsset: network.nativeToken.symbol,
        callDataAmount: "0",
        toAddress: htlcContractAddress,
    };
}

export async function createCommitCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {
    const commitRequest = decodeJson<HTLCCommitTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === commitRequest.sourceAsset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.sourceAsset}`)
    };

    const createEmptyArray = (length: number, char: string) =>
        Array.from({ length }, () => ''.padEnd(64, char));

    const hopChains = createEmptyArray(5, ' ');
    const hopAssets = createEmptyArray(5, ' ');
    const hopAddresses = createEmptyArray(5, ' ');

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const provider = new Provider(network.nodes[0].url);
    const contractInstance = new Contract(htlcContractAddress, abi, provider);
    const receiverAddress = { bits: commitRequest.receiver };
    const assetId: AssetId = new Address(token.contract).toAssetId();

    const callConfig = contractInstance.functions
        .commit(
            hopChains,
            hopAssets,
            hopAddresses,
            commitRequest.destinationChain.padEnd(64, ' '),
            commitRequest.destinationAsset.padEnd(64, ' '),
            RemoveHexPrefix(commitRequest.destinationAddress).padEnd(64, ' '),
            commitRequest.sourceAsset.padEnd(64, ' '),
            commitRequest.id,
            receiverAddress,
            DateTime.fromUnixSeconds(commitRequest.timelock).toTai64())
        .callParams({
            forward: [Number(commitRequest.amount), assetId.bits]
        })
        .txParams({
            maxFee: bn(1000000),
        });

    const txRequest = await callConfig.getTransactionRequest();

    return {
        data: JSON.stringify(txRequest),
        amount: "0",
        asset: network.nativeToken.symbol,
        callDataAsset: token.symbol,
        callDataAmount: "0",
        toAddress: htlcContractAddress,
    };
}

export async function createRedeemCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

    const redeemRequest = decodeJson<HTLCRedeemTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === redeemRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${redeemRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const provider = new Provider(network.nodes[0].url);
    const contractInstance = new Contract(htlcContractAddress, abi, provider);

    const callConfig = contractInstance.functions
        .redeem(redeemRequest.commitId, redeemRequest.secret)
        .txParams({
            maxFee: bn(1000000),
        });

    const txRequest = await callConfig.getTransactionRequest();

    return {
        data: JSON.stringify(txRequest),
        amount: "0",
        asset: network.nativeToken.symbol,
        callDataAsset: network.nativeToken.symbol,
        callDataAmount: "0",
        toAddress: htlcContractAddress,
    };
}

export async function createLockCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === lockRequest.sourceAsset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.sourceAsset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const provider = new Provider(network.nodes[0].url);
    const contractInstance = new Contract(htlcContractAddress, abi, provider);

    const receiverAddress = { bits: lockRequest.receiver };

    const assetId: AssetId = new Address(token.contract).toAssetId();
    const sendAmount = Number(lockRequest.amount) + Number(lockRequest.reward);

    const callConfig = contractInstance.functions
        .lock(
            lockRequest.commitId,
            lockRequest.hashlock,
            lockRequest.reward,
            DateTime.fromUnixSeconds(lockRequest.rewardTimelock).toTai64(),
            DateTime.fromUnixSeconds(lockRequest.timelock).toTai64(),
            receiverAddress,
            lockRequest.sourceAsset.padEnd(64, ' '),
            lockRequest.destinationNetwork.padEnd(64, ' '),
            lockRequest.destinationAsset.padEnd(64, ' '),
            RemoveHexPrefix(lockRequest.destinationAddress).padEnd(64, ' '),
        ).callParams({
            forward: [sendAmount, assetId.bits],
        })
        .txParams({
            maxFee: bn(1000000),
        });

    const txRequest = await callConfig.getTransactionRequest();
   
    return {
        data: JSON.stringify(txRequest),
        amount: sendAmount.toString(),
        asset: lockRequest.sourceAsset,
        callDataAsset: lockRequest.sourceAsset,
        callDataAmount: sendAmount.toString(),
        toAddress: htlcContractAddress,
    };
}

export async function createAddLockSigCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

    const addLockSigRequest = decodeJson<HTLCAddLockSigTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.symbol === addLockSigRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${addLockSigRequest.asset}`)
    };

    const htlcContractAddress = token.contract
        ? network.htlcNativeContractAddress
        : network.htlcTokenContractAddress

    const provider = new Provider(network.nodes[0].url);

    const contractInstance = new Contract(htlcContractAddress, abi, provider);

    const callConfig = contractInstance.functions
        .add_lock_sig(
            addLockSigRequest.signature,
            addLockSigRequest.commitId,
            addLockSigRequest.hashlock,
            DateTime.fromUnixSeconds(addLockSigRequest.timelock).toTai64()
        )
        .txParams({
            maxFee: bn(1000000),
        });

    const txRequest = await callConfig.getTransactionRequest();

    return {
        data: JSON.stringify(txRequest),
        amount: "0",
        asset: network.nativeToken.symbol,
        callDataAsset: network.nativeToken.symbol,
        callDataAmount: "0",
        toAddress: htlcContractAddress,
    };
}

export async function createTransferCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

    const transferRequest = decodeJson<TransferPrepareRequest>(args);
    const token = network.tokens.find(t => t.symbol === transferRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${transferRequest.asset}`)
    };

    const provider = new Provider(network.nodes[0].url);
    const wallet = Wallet.fromAddress(transferRequest.fromAddress, provider);

    const transactionRequest: ScriptTransactionRequest = await wallet.createTransfer(
        transferRequest.toAddress,
        transferRequest.amount,
        token.contract,
    );

    return {
        data: JSON.stringify(transactionRequest),
        amount: transferRequest.amount.toString(),
        asset: token.symbol,
        callDataAsset: token.symbol,
        callDataAmount: transferRequest.amount.toString(),
        toAddress: transferRequest.toAddress,
    };

}

function PadStringsTo64(input: string[]): string[] {
    return input.map(str => str.padEnd(64, ' '));
}

function RemoveHexPrefix(hex: string): string {
  return hex.replace(/^0x/i, "");
}
