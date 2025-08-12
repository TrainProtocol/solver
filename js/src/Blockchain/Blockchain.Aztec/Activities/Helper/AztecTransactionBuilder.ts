import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { HTLCCommitTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCCommitTransactionPrepareRequest";
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { AztecAddress, ContractArtifact, FunctionAbi, getAllFunctionAbis, loadContractArtifact, NoirCompiledContract } from "@aztec/aztec.js";
import { AztecPrepareTransactionResponse } from "../../Models/AztecPrepareTransactionResponse";
import { TokenContract } from '@aztec/noir-contracts.js/Token';
import trainContract from "../ABIs/train.json";

export async function createRefundCallData(network: DetailedNetworkDto, args: string): Promise<AztecPrepareTransactionResponse> {

  const refundRequest = decodeJson<HTLCRefundTransactionPrepareRequest>(args);

  const token = network.tokens.find(t => t.symbol === refundRequest.asset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${refundRequest.asset}`)
  };

  const htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress;

  const functionInteractionAbi = getFunctionAbi(loadContractArtifact(trainContract as NoirCompiledContract), 'refund_private');

  const contractFunctionInteraction = {
    contractAddress: AztecAddress.fromString(htlcContractAddress),
    functionAbi: functionInteractionAbi,
    args: [refundRequest.commitId]
  }

  return {
    functionInteractions: [contractFunctionInteraction],
    amount: 0,
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
    callDataAmount: 0,
    toAddress: htlcContractAddress,
  };
}

export async function createCommitCallData(network: DetailedNetworkDto, args: string): Promise<AztecPrepareTransactionResponse> {
  const commitRequest = decodeJson<HTLCCommitTransactionPrepareRequest>(args);

  const token = network.tokens.find(t => t.symbol === commitRequest.sourceAsset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${commitRequest.sourceAsset}`)
  };
  const tokenContractArtifact = TokenContract.artifact;

  let htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress


  const functionInteractionAbi = getFunctionAbi(loadContractArtifact(trainContract as NoirCompiledContract), 'commit_private_user');
  const transferFunctionInteractionAbi = getFunctionAbi(tokenContractArtifact, 'transfer_to_public');

  const transferFunctionInteraction = {
    wallet: null,
    contractAddress: AztecAddress.fromString(token.contract),
    functionAbi: transferFunctionInteractionAbi,
    args: [
      AztecAddress.fromString(htlcContractAddress),
      commitRequest.amount,
      commitRequest.randomness
    ]
  };

  let contractFunctionInteraction = {
    wallet: null,
    contractAddress: AztecAddress.fromString(htlcContractAddress),
    functionAbi: functionInteractionAbi,
    args: [
      commitRequest.commitId,
      AztecAddress.fromString(htlcContractAddress),
      commitRequest.timelock,
      AztecAddress.fromString(token.contract),
      commitRequest.amount,
      commitRequest.destinationChain,
      commitRequest.destinationAsset,
      commitRequest.destinationAddress,
      commitRequest.randomness
    ]
  }

  return {
    functionInteractions: [contractFunctionInteraction, transferFunctionInteraction],
    amount: 0,
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
    callDataAmount: 0,
    toAddress: htlcContractAddress,
  };
}

export async function createRedeemCallData(network: DetailedNetworkDto, args: string): Promise<AztecPrepareTransactionResponse> {

  const redeemRequest = decodeJson<HTLCRedeemTransactionPrepareRequest>(args);
  const token = network.tokens.find(t => t.symbol === redeemRequest.asset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${redeemRequest.asset}`)
  };

  const htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress

  const functionAbi = getFunctionAbi(loadContractArtifact(trainContract as NoirCompiledContract), 'redeem_private');

  let functionInteraction = {
    wallet: null,
    contractAddress: AztecAddress.fromString(htlcContractAddress),
    functionAbi: functionAbi,
    args: [redeemRequest.commitId]
  }

  return {
    functionInteractions: [functionInteraction],
    amount: 0,
    asset: network.nativeToken.symbol,
    callDataAsset: token.symbol,
    callDataAmount: 0,
    toAddress: htlcContractAddress,
  };
}

export async function createLockCallData(network: DetailedNetworkDto, args: string): Promise<AztecPrepareTransactionResponse> {

  const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(args);

  const token = network.tokens.find(t => t.symbol === lockRequest.sourceAsset);

  if (!token) {
    throw new Error(`Token not found for network ${network.name} and asset ${lockRequest.sourceAsset}`)
  };

  const htlcContractAddress = token.contract
    ? network.htlcNativeContractAddress
    : network.htlcTokenContractAddress

  const tokenContractArtifact = TokenContract.artifact;

  const functionInteractionAbi = getFunctionAbi(loadContractArtifact(trainContract as NoirCompiledContract), 'lock_private_solver');
  const transferFunctionInteractionAbi = getFunctionAbi(tokenContractArtifact, 'transfer_to_public');

  const transferFunctionInteraction = {
    wallet: null,
    contractAddress: AztecAddress.fromString(token.contract),
    functionAbi: transferFunctionInteractionAbi,
    args: [
      AztecAddress.fromString(htlcContractAddress),
      lockRequest.amount,
      lockRequest.randomness
    ]
  };

  let contractFunctionInteraction = {
    wallet: null,
    contractAddress: AztecAddress.fromString(htlcContractAddress),
    functionAbi: functionInteractionAbi,
    args: [
      lockRequest.commitId,
      AztecAddress.fromString(htlcContractAddress),
      lockRequest.timelock,
      AztecAddress.fromString(token.contract),
      lockRequest.amount,
      lockRequest.destinationNetwork,
      lockRequest.destinationAsset,
      lockRequest.destinationAddress,
      lockRequest.randomness
    ]
  }

  return {
    functionInteractions: [contractFunctionInteraction, transferFunctionInteraction],
    amount: lockRequest.amount + lockRequest.reward,
    asset: lockRequest.sourceAsset,
    callDataAsset: lockRequest.sourceAsset,
    callDataAmount: lockRequest.amount + lockRequest.reward,
    toAddress: htlcContractAddress,
  };
}

function getFunctionAbi(
  artifact: ContractArtifact,
  fnName: string,
): FunctionAbi {
  const fn = getAllFunctionAbis(artifact).find(({ name }) => name === fnName);
  if (!fn) { }
  return fn;
}