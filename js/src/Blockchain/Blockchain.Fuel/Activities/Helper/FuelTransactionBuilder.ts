import { utils } from "ethers";
import { ContractType } from "../../../../Data/Entities/Contracts";
import { Networks } from "../../../../Data/Entities/Networks";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCAddLockSigTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCAddLockSigTransactionPrepareRequest";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { Contract, DateTime, Provider } from "fuels";
import { NodeType } from "../../../../Data/Entities/Nodes";
import abi from '../ABIs/ERC20.json';

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
        .getCallConfig();

    return {
        Data: JSON.stringify(callConfig),
        Amount: 0,
        AmountInWei: "0",
        Asset: nativeToken.asset,
        CallDataAsset: token.asset,
        CallDataAmountInWei: "0",
        CallDataAmount: 0,
        ToAddress: htlcContractAddress.address,
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
        .getCallConfig();

    return {
        Data: JSON.stringify(callConfig),
        Amount: 0,
        AmountInWei: "0",
        Asset: nativeToken.asset,
        CallDataAsset: token.asset,
        CallDataAmountInWei: "0",
        CallDataAmount: 0,
        ToAddress: htlcContractAddress.address,
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

    const callConfig = contractInstance.functions
        .lock(
            lockRequest.Id,
            lockRequest.Hashlock,
            lockRequest.Reward,
            DateTime.fromUnixSeconds(lockRequest.RewardTimelock).toTai64(),
            DateTime.fromUnixSeconds(lockRequest.Timelock).toTai64(),
            lockRequest.Receiver,
            lockRequest.SourceAsset,
            lockRequest.DestinationNetwork,
            lockRequest.DestinationAsset,
            lockRequest.DestinationAddress,
        ).callParams({
            forward: [lockRequest.Amount + lockRequest.Reward, lockRequest.SourceAsset],
        })
        .getCallConfig();

    return {
        Data: JSON.stringify(callConfig),
        Amount: lockRequest.Amount + lockRequest.Reward,
        AmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
        Asset: lockRequest.SourceAsset,
        CallDataAsset: lockRequest.SourceAsset,
        CallDataAmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),
        CallDataAmount: lockRequest.Amount + lockRequest.Reward,
        ToAddress: htlcContractAddress.address,
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
        .getCallConfig();

    return {
        Data: JSON.stringify(callConfig),
        Amount: 0,
        AmountInWei: "0",
        Asset: nativeToken.asset,
        CallDataAsset: token.asset,
        CallDataAmountInWei: '0',
        CallDataAmount: 0,
        ToAddress: htlcContractAddress.address,
    };
}