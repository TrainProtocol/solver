import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { AztecAddress, Fr } from "@aztec/aztec.js";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import crypto from 'crypto';

export async function createRefundCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

  const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);
  const token = network.tokens.find(t => t.symbol === refundRequest.asset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.asset}`)
  };

  const htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress;

  let functionInteraction: FunctionInteraction = {
    interactionAddress: htlcContractAddress,
    functionName: "refund_private",
    args: [refundRequest.commitId],
  }

  const json = JSON.stringify(functionInteraction);

  return {
    data: json,
    amount: "0",
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
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

  let htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress;

  const randomness = BigInt('0x' + crypto.randomBytes(31).toString('hex')).toString()

  let functionInteraction: FunctionInteraction = {
    interactionAddress: htlcContractAddress,
    functionName: "commit_private_user",
    args: [
      BigInt(commitRequest.id).toString(),
      commitRequest.receiver,
      commitRequest.timelock,
      AztecAddress.fromString(token.contract),
      commitRequest.amount,
      commitRequest.sourceAsset.padStart(30, ' '),
      commitRequest.destinationChain.padStart(30, ' '),
      commitRequest.destinationAsset.padStart(30, ' '),
      commitRequest.destinationAddress.padStart(90, ' '),
      randomness,
    ],
    authwiths: [
      {
        interactionAddress: token.contract,
        functionName: "transfer_to_public",
        callerAddress: htlcContractAddress,
        args: [
          AztecAddress.fromString(htlcContractAddress).toString(),
          commitRequest.amount,
          randomness],
      }]
  }

  const json = JSON.stringify(functionInteraction);

  return {
    data: json,
    amount: "0",
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
    callDataAmount: "0",
    toAddress: htlcContractAddress,
  };
}

export async function createRedeemCallData(network: DetailedNetworkDto, args: string): Promise<PrepareTransactionResponse> {

  const redeemRequest =   decodeJson<HTLCRedeemTransactionPrepareRequest>(args);
  const token = network.tokens.find(t => t.symbol === redeemRequest.asset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${redeemRequest.asset}`)
  };

  const htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress

  const toHex = (n: bigint) => '0x' + n.toString(16);
  const [secretHigh, secretLow] = hexToU128Limbs(toHex(BigInt(redeemRequest.secret)));
  const [ownershipKeyHigh, ownershipKeyLow] = hexToU128Limbs(toHex(BigInt(redeemRequest.secret)));

  let functionInteraction: FunctionInteraction = {
    interactionAddress: htlcContractAddress,
    functionName: "redeem_private",
    args: [
      Fr.fromString(redeemRequest.commitId).toString(),
      secretHigh.toString(),
      secretLow.toString(),
      ownershipKeyHigh.toString(),
      ownershipKeyLow.toString()
    ],
  }

  const json = JSON.stringify(functionInteraction);

  return {
    data: json,
    amount: "0",
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
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

  const randomness = BigInt('0x' + crypto.randomBytes(31).toString('hex')).toString()
  const hashlock = hexToU128Limbs(lockRequest.hashlock);
  const ownershipHash = hexToU128Limbs(normalizeHex(lockRequest.receiver));

  let functionInteraction: FunctionInteraction = {
    interactionAddress: htlcContractAddress,
    functionName: "lock_private_solver",
    args: [
      BigInt(lockRequest.commitId).toString(),
      hashlock[0].toString(),
      hashlock[1].toString(),
      lockRequest.amount,
      ownershipHash[0].toString(),
      ownershipHash[1].toString(),
      lockRequest.timelock,
      AztecAddress.fromString(token.contract),
      randomness,
      lockRequest.sourceAsset.padStart(30, ' '),
      lockRequest.destinationNetwork.padStart(30, ' '),
      lockRequest.destinationAsset.padStart(30, ' '),
      lockRequest.destinationAddress.padStart(90, ' ')],
    authwiths: [
      {
        interactionAddress: token.contract,
        functionName: "transfer_to_public",
        callerAddress: htlcContractAddress,
        args: [
          AztecAddress.fromString(htlcContractAddress).toString(),
          lockRequest.amount,
          randomness],
      }]
  }

  const json = JSON.stringify(functionInteraction);

  return {
    data: json,
    amount: (lockRequest.amount + lockRequest.reward).toString(),
    asset: lockRequest.sourceAsset,
    callDataAsset: lockRequest.sourceAsset,
    callDataAmount: (lockRequest.amount + lockRequest.reward).toString(),
    toAddress: htlcContractAddress,
  };
}

interface FunctionInteraction {
  interactionAddress: string,
  functionName: string,
  args: any[],
  callerAddress?: string,
  authwiths?: FunctionInteraction[],
}

export const hexToUint256HexStrings = (hex: string): string[] => {
  let h = hex.startsWith('0x') ? hex.slice(2) : hex;
  if (h.length % 2) h = '0' + h;
  const pad = (64 - (h.length % 64)) % 64;
  h = '0'.repeat(pad) + h;
  const out: string[] = [];
  for (let i = 0; i < h.length; i += 64) out.push('0x' + h.slice(i, i + 64));
  return out;
};

export const hexToUint256Array = (hex: string): bigint[] => {
  let h = hex.startsWith('0x') ? hex.slice(2) : hex;
  if (h.length % 2) h = '0' + h;
  const pad = (64 - (h.length % 64)) % 64;
  h = '0'.repeat(pad) + h;
  const out: bigint[] = [];
  for (let i = 0; i < h.length; i += 64) out.push(BigInt('0x' + h.slice(i, i + 64)));
  return out;
};

function hexToU128Limbs(hex: string): [bigint, bigint] {
  const bytes = hex.replace('0x', '').match(/.{2}/g)!.map(b => parseInt(b, 16));
  let high = BigInt(0), low = BigInt(0);
  for (let i = 0; i < 16; i++) high = (high << BigInt(8)) + BigInt(bytes[i]);
  for (let i = 16; i < 32; i++) low = (low << BigInt(8)) + BigInt(bytes[i]);
  return [high, low];
}

function normalizeHex(hex: string): string {
  let value = hex.trim()
  if (value.toLowerCase().startsWith("0x")) {
    return value
  }
  return "0x" + value
}