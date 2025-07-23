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

    const token = network.tokens.find(t => t.asset === refundRequest.Asset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.Asset}`);
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
        .refund(refundRequest.Id)
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

    const token = network.tokens.find(t => t.asset === commitRequest.SourceAsset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.SourceAsset}`);
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
    const receiverAddress = { bits: commitRequest.Receiver };

    const callConfig = contractInstance.functions
        .commit(
            PadStringsTo64(commitRequest.HopChains),
            PadStringsTo64(commitRequest.HopAssets),
            PadStringsTo64(commitRequest.HopAddresses),
            commitRequest.DestinationChain.padEnd(64, ' '),
            commitRequest.DestinationAddress.padEnd(64, ' '),
            commitRequest.SourceAsset.padEnd(64, ' '),
            commitRequest.Id,
            receiverAddress,
            DateTime.fromUnixSeconds(commitRequest.Timelock).toTai64())
        .callParams({
            forward: [Number(formatUnits(commitRequest.Amount, token.decimals)), await provider.getBaseAssetId()]
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

    const token = network.tokens.find(t => t.asset === redeemRequest.Asset);

    if (!token) {
        throw new Error(`Token not found for network ${network.name} and assets ${redeemRequest.Asset}`);
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
        .redeem(redeemRequest.Id, redeemRequest.Secret)
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

    const token = network.tokens.find(t => t.asset === lockRequest.SourceAsset);
    if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.SourceAsset}`)
    };

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);
    const provider = new Provider(node.url);
    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

    const receiverAddress = { bits: lockRequest.Receiver };

    const b256: B256Address = token.tokenContract;
    const address: Address = Address.fromB256(b256);
    const assetId: AssetId = address.toAssetId();

    const callConfig = contractInstance.functions
        .lock(
            lockRequest.Id,
            lockRequest.Hashlock,
            lockRequest.Reward,
            DateTime.fromUnixSeconds(lockRequest.RewardTimelock).toTai64(),
            DateTime.fromUnixSeconds(lockRequest.Timelock).toTai64(),
            receiverAddress,
            lockRequest.SourceAsset.padEnd(64, ' '),
            lockRequest.DestinationNetwork.padEnd(64, ' '),
            lockRequest.DestinationAsset.padEnd(64, ' '),
            lockRequest.DestinationAddress.padEnd(64, ' '),
        ).callParams({
            forward: [Number(formatUnits(lockRequest.Amount + lockRequest.Reward, token.decimals)), assetId.bits],
        })
        .txParams({
            maxFee: bn(1000000),
        });

    return {
        data: JSON.stringify(callConfig),
        amount: lockRequest.Amount + lockRequest.Reward,
        AmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
        asset: lockRequest.SourceAsset,
        callDataAsset: lockRequest.SourceAsset,
        CallDataAmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
        callDataAmount: lockRequest.Amount + lockRequest.Reward,
        toAddress: htlcContractAddress.address,
    };
}

export async function CreateAddLockSigCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

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

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
        throw new Error(`Primary node not found for network ${network.name}`);
    }

    const provider = new Provider(node.url);

    const contractInstance = new Contract(htlcContractAddress.address, abi, provider);

    const callConfig = contractInstance.functions
        .add_lock_sig(
            addLockSigRequest.Signature,
            addLockSigRequest.Id,
            addLockSigRequest.Hashlock,
            DateTime.fromUnixSeconds(addLockSigRequest.Timelock).toTai64()
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