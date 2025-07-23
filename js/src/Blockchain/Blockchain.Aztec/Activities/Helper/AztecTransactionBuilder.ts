import { ContractType } from "../../../../Data/Entities/Contracts";
import { Networks } from "../../../../Data/Entities/Networks";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { NodeType } from "../../../../Data/Entities/Nodes";
import { AztecAddress, Fr } from '@aztec/aztec.js';
import { randomBytes } from 'crypto';
import { AztecTransactionPrepareRequest } from "../../Models/AztecTransactionPrepareRequest.ts";
import { utils } from "ethers";

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

  const functionName = "refund_private";
  const Id = Fr.fromString(refundRequest.Id);

  const callData: AztecTransactionPrepareRequest = {
    functionName: functionName,
    functionParams: [Id]
  }

  return {
    Data: callData,
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

  const functionName = "redeem_private";
  const Id = Fr.fromString(redeemRequest.Id);
  const secret = Array.from(new TextEncoder().encode("secret"));
  const ownershipKey = Array.from(new TextEncoder().encode("secret"));

  const callData: AztecTransactionPrepareRequest = {
    functionName: functionName,
    functionParams: [
      Id,
      secret,
      ownershipKey
    ]
  }

  return {
    Data: callData,
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
  const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

  const token = network.tokens.find(t => t.asset === lockRequest.SourceAsset);
  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.SourceAsset}`)
  };

  const node = network.nodes.find(n => n.type === NodeType.Primary);
  if (!node) {
    throw new Error(`Primary node not found for network ${network.name}`);
  }

  const Id = Fr.fromString(lockRequest.Id);
  const hashlock = Array.from(new TextEncoder().encode());
  const amount = lockRequest.Amount;
  const ownership_hash = Array.from(new TextEncoder().encode(lockRequest.OwnershipHash));
  const timelock = lockRequest.timeLock;
  const randomness = randomBytes(32).toString(lockRequest.Randomness);
  const dst_chain = lockRequest.DestinationNetwork.padEnd(30, ' ');
  const dst_asset = lockRequest.DestinationAsset.padEnd(30, ' ');
  const dst_address = lockRequest.DestinationAddress.padEnd(90, ' ');

  const functionName = 'lock_private_solver';
  const callData: AztecTransactionPrepareRequest = {
    functionName: functionName,
    functionParams: [
      Id,
      hashlock,
      amount,
      ownership_hash,
      timelock,
      AztecAddress.fromString(token.tokenContract),
      randomness,
      dst_chain,
      dst_asset,
      dst_address,
    ]
  };

  return {
    Data: callData,
    Amount: 0,
    AmountInWei: "0",
    Asset: lockRequest.SourceAsset,
    CallDataAsset: lockRequest.SourceAsset,
    CallDataAmountInWei: utils.parseUnits((lockRequest.Amount + lockRequest.Reward).toString(), token.decimals).toString(),//need to clarify about reward time
    CallDataAmount: lockRequest.Amount + lockRequest.Reward,
    ToAddress: htlcContractAddress.address,
  };
}

export async function CreateCommitCallData(network: Networks, args: string): Promise<PrepareTransactionResponse> {

  const commitRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);
  const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);

  const token = network.tokens.find(t => t.asset === commitRequest.SourceAsset);
  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.SourceAsset}`)
  };

  const node = network.nodes.find(n => n.type === NodeType.Primary);
  if (!node) {
    throw new Error(`Primary node not found for network ${network.name}`);
  }

  const functionName = 'commit_private_user';
  const Id = Fr.fromString(commitRequest.Id);
  const amount = commitRequest.Amount;
  const timelock = commitRequest.timeLock;
  const randomness = randomBytes(32).toString(commitRequest.Randomness);
  const dst_chain = commitRequest.DestinationNetwork.padEnd(30, ' ');
  const dst_asset = commitRequest.DestinationAsset.padEnd(30, ' ');
  const dst_address = commitRequest.DestinationAddress.padEnd(90, ' ');
  let solverAddress = AztecAddress.fromString(commitRequest.FromAddress);

  const callData: AztecTransactionPrepareRequest = {
    functionName: functionName,
    functionParams: [
      Id,
      solverAddress,
      timelock,
      AztecAddress.fromString(token.tokenContract),
      amount,
      dst_chain,
      dst_asset,
      dst_address,
      randomness,
    ]
  };

  return {
    Data: callData,
    Amount: 0,
    AmountInWei: "0",
    Asset: commitRequest.SourceAsset,
    CallDataAsset: commitRequest.SourceAsset,
    CallDataAmountInWei: utils.parseUnits((commitRequest.Amount + commitRequest.Reward).toString(), token.decimals).toString(),//need to clarify about reward time
    CallDataAmount: commitRequest.Amount + commitRequest.Reward,
    ToAddress: htlcContractAddress.address,
  };
}