import { utils } from "ethers";
import { ContractType } from "../../../../Data/Entities/Contracts";
import { Networks } from "../../../../Data/Entities/Networks";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { Address, AssetId, B256Address, bn, Contract, DateTime, formatUnits, Provider, Wallet } from "fuels";
import { NodeType } from "../../../../Data/Entities/Nodes";
import abi from '../ABIs/train.json';

export async function CreateRefundCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

    const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

    const token = network.tokens.find(t => t.asset === refundRequest.asset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.asset}`);
    }

    const nativeToken = network.tokens.find(t => t.isNative === true);
    if (!nativeToken) {
        throw new Error(`Native token not found for network ${network.name}`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const provider = new Provider(node.url);
    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

    const callConfig = contractInstance.functions
        .refund(refundRequest.commitId)
        .txParams({
            maxFee: bn(1000000),
        });

    return {
        data: JSON.stringify(callConfig),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: "0",
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export async function CreateCommitCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {
    const commitRequest = decodeJson<HTLCCommitTransactionPrepareRequest>(args);

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

    const token = network.tokens.find(t => t.asset === commitRequest.sourceAsset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.sourceAsset}`);
    }

    const nativeToken = network.tokens.find(t => t.isNative === true);
    if (!nativeToken) {
        throw new Error(`Native token not found for network ${network.name}`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const provider = new Provider(node.url);
    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);
    const receiverAddress = { bits: commitRequest.receiver };

    const callConfig = contractInstance.functions
        .commit(
            PadStringsTo64(commitRequest.hopChains),
            PadStringsTo64(commitRequest.hopAssets),
            PadStringsTo64(commitRequest.hopAddresses),
            commitRequest.destinationChain.padEnd(64, ' '),
            commitRequest.destinationAddress.padEnd(64, ' '),
            commitRequest.sourceAsset.padEnd(64, ' '),
            commitRequest.commitId,
            receiverAddress,
            DateTime.fromUnixSeconds(commitRequest.timelock).toTai64())
        .callParams({
            forward: [Number(formatUnits(commitRequest.amount, token.decimals)), await provider.getBaseAssetId()]
        })
        .txParams({
            maxFee: bn(1000000),
        });

    return {
        data: JSON.stringify(callConfig),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: "0",
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export async function CreateRedeemCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

    const redeemRequest = decodeJson<HTLCRedeemTransactionPrepareRequest>(args);

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

    const token = network.tokens.find(t => t.asset === redeemRequest.asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and assets ${redeemRequest.asset}`);
    }

    const nativeToken = network.tokens.find(t => t.isNative === true);

    if (!nativeToken) {
        throw new Error(`Native token not found for network ${network.name}`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const provider = new Provider(node.url);
    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

    const callConfig = contractInstance.functions
        .redeem(redeemRequest.commitId, redeemRequest.Secret)
        .txParams({
            maxFee: bn(1000000),
        });

    return {
        data: JSON.stringify(callConfig),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: "0",
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

export async function CreateLockCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);

    const token = network.tokens.find(t => t.asset === lockRequest.sourceAsset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.sourceAsset}`)
    };

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);
    const provider = new Provider(node.url);
    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

    const receiverAddress = { bits: lockRequest.receiver };

    const b256: B256Address = token.tokenContract;
    const address: Address = Address.fromB256(b256);
    const assetId: AssetId = address.toAssetId();

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
            lockRequest.destinationAddress.padEnd(64, ' '),
        ).callParams({
            forward: [Number(formatUnits(lockRequest.amount + lockRequest.reward, token.decimals)), assetId.bits],
        })
        .txParams({
            maxFee: bn(1000000),
        });

    return {
        data: JSON.stringify(callConfig),
        amount: lockRequest.amount + lockRequest.reward,
        AmountInWei: utils.parseUnits((lockRequest.amount + lockRequest.reward).toString(), token.decimals).toString(),
        asset: lockRequest.sourceAsset,
        callDataAsset: lockRequest.sourceAsset,
        CallDataAmountInWei: utils.parseUnits((lockRequest.amount + lockRequest.reward).toString(), token.decimals).toString(),
        callDataAmount: lockRequest.amount + lockRequest.reward,
        toAddress: htlcContractAddress.address,
    };
}

export async function CreateAddLockSigCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

    const addLockSigRequest = decodeJson<HTLCAddLockSigTransactionPrepareRequest>(args);

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);
    const token = network.tokens.find(t => t.asset === addLockSigRequest.asset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${addLockSigRequest.asset}`);
    }

    const nativeToken = network.tokens.find(t => t.isNative === true);
    if (!nativeToken) {
        throw new Error(`Native token not found for network ${network.name}`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const provider = new Provider(node.url);

    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

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

    return {
        data: JSON.stringify(callConfig),
        amount: 0,
        AmountInWei: "0",
        asset: nativeToken.asset,
        callDataAsset: token.asset,
        CallDataAmountInWei: '0',
        callDataAmount: 0,
        toAddress: htlcContractAddress.address,
    };
}

function PadStringsTo64(input: string[]): string[] {
    return input.map(str => str.padEnd(64, ' '));
}