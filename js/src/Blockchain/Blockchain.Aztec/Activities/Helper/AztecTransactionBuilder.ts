import { ContractType } from "../../../../Data/Entities/Contracts";
import { Networks } from "../../../../Data/Entities/Networks";
import { decodeJson } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { HTLCLockTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { HTLCRedeemTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRedeemTransactionPrepareRequest";
import { HTLCRefundTransactionPrepareRequest } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCRefundTransactionPrepareRequest";
import { PrepareTransactionResponse } from "../../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { NodeType } from "../../../../Data/Entities/Nodes";
import { AztecAddress, Contract, createAztecNodeClient, Fr, getContractInstanceFromDeployParams, loadContractArtifact, SponsoredFeePaymentMethod, } from '@aztec/aztec.js';
import { TrainContract, TrainContractArtifact } from './Train.ts';
import { getSchnorrWallet } from '@aztec/accounts/schnorr';
import { deriveSigningKey } from '@aztec/stdlib/keys';
import { getPXEServiceConfig } from "@aztec/pxe/config";
import { createPXEService } from "@aztec/pxe/server";
import { SponsoredFPCContract } from '@aztec/noir-contracts.js/SponsoredFPC';
import { TokenContract } from "@aztec/noir-contracts.js/Token";
import { randomBytes } from 'crypto';
import { readFileSync } from 'fs';


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
  

  return {
    Data: JSON.stringify(" "),
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

  let userSecretKey = Fr.fromString("privateKey");

  const provider = createAztecNodeClient(node);

  const fullConfig = {
    ...getPXEServiceConfig(),
    l1Contracts: await provider.getL1ContractAddresses(),
  };

  const pxe = await createPXEService(node, fullConfig);
  const schnorrWallet = await getSchnorrWallet(
    pxe,
    AztecAddress.fromString(redeemRequest.FromAddress),
    deriveSigningKey(userSecretKey)
  );

  const Id = Fr.fromString(redeemRequest.Id);
  const secret = Array.from(new TextEncoder().encode("secret"));
  const ownershipKey = Array.from(new TextEncoder().encode("secret"));

  const contractInstance = await TrainContract.at(
    AztecAddress.fromString(htlcContractAddress),
    schnorrWallet,
  );

  const callConfig = await contractInstance.methods.redeem_private(Id, secret, ownershipKey).simulate();

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

  let userSecretKey = Fr.fromString("privateKey");
  const provider = createAztecNodeClient(node);
  const fullConfig = {
    ...getPXEServiceConfig(),
    l1Contracts: await provider.getL1ContractAddresses(),
  };

  const pxe = await createPXEService(node, fullConfig);
  const schnorrWallet = await getSchnorrWallet(
    pxe,
    AztecAddress.fromString(lockRequest.FromAddress),
    deriveSigningKey(userSecretKey)
  );


  const Id = Fr.fromString(lockRequest.Id);
  const hashlock = Array.from(new TextEncoder().encode("hashLock"));
  const amount = lockRequest.Amount;
  const ownership_hash = Array.from(new TextEncoder().encode("secret"));
  const timelock = lockRequest.timeLock;
  const randomness = randomBytes(32).toString('hex');
  const dst_chain = lockRequest.DestinationNetwork.padEnd(8, ' ');
  const dst_asset = lockRequest.DestinationAsset.padEnd(8, ' ');
  const dst_address = lockRequest.DestinationAddress.padEnd(8, ' ');

  // Token contract operations using auth witness
  const TokenContractArtifact = TokenContract.artifact;
  const asset = await Contract.at(
    AztecAddress.fromString(token),
    TokenContractArtifact,
    schnorrWallet,
  );

  // const callConfig = asset
  //   .withWallet(schnorrWallet)
  //   .methods.transfer_to_public(
  //     schnorrWallet.getAddress(),
  //     AztecAddress.fromString(htlcContractAddress),
  //     amount,
  //     randomness,
  //   ).simulate;

  const transfer = asset
    .withWallet(schnorrWallet)
    .methods.transfer_to_public(
      schnorrWallet.getAddress(),
      AztecAddress.fromString(htlcContractAddress),
      amount,
      randomness,
    );
  const witness = await schnorrWallet.createAuthWit({
    caller: AztecAddress.fromString(htlcContractAddress),
    action: transfer,
  });


  const contract = await Contract.at(
    AztecAddress.fromString(htlcContractAddress),
    TrainContractArtifact,
    schnorrWallet,
  );
  const is_contract_initialized = await contract.methods
    .is_contract_initialized(Id)
    .simulate();
  if (is_contract_initialized) throw new Error('HTLC Exsists');
  const lockTx = await contract.methods
    .lock_private_solver(
      Id,
      hashlock,
      amount,
      ownership_hash,
      timelock,
      token,
      null,//randomness,
      dst_chain,
      dst_asset,
      dst_address,
    )
    .send({ authWitnesses: [witness], fee: {} })//paymentMethod
    .wait();

}