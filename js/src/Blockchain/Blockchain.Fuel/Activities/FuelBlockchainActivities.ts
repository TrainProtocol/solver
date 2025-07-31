import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee, FixedFeeData, LegacyFeeData } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { Contract, BigNumberCoder, Provider, TransactionCost, Wallet, Signer, sha256, DateTime, bn, hashMessage, B256Coder, concat, Address, B256Address, AssetId, isTransactionTypeScript, transactionRequestify, ScriptTransactionRequest } from "fuels";
import abi from './ABIs/train.json';
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { IFuelBlockchainActivities } from "./IFuelBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { utils } from "ethers";
import TrackBlockEventsAsync from "./Helper/FuelEventTracker";
import { createAddLockSigCallData, createRefundCallData, createLockCallData, createRedeemCallData, createCommitCallData } from "./Helper/FuelTransactionBuilder";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { PrivateKeyRepository } from "../../Blockchain.Abstraction/Models/WalletsModels/PrivateKeyRepository";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { mapFuelStatusToInternal } from "./Helper/FuelTransactionStatusMapper";
import { inject, injectable } from "tsyringe";
import { TreasuryClient } from "../../Blockchain.Abstraction/Infrastructure/TreasuryClient/treasuryClient";

@injectable()
export class FuelBlockchainActivities implements IFuelBlockchainActivities {
  constructor(
    @inject("TreasuryClient") private treasuryClient: TreasuryClient
  ) { }

  public async BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
    try {
      switch (request.type) {
        case TransactionType.HTLCLock:
          return createLockCallData(request.network, request.prepareArgs);
        case TransactionType.HTLCRedeem:
          return createRedeemCallData(request.network, request.prepareArgs);
        case TransactionType.HTLCRefund:
          return createRefundCallData(request.network, request.prepareArgs);
        case TransactionType.HTLCAddLockSig:
          return createAddLockSigCallData(request.network, request.prepareArgs);
        case TransactionType.HTLCCommit:
          return createCommitCallData(request.network, request.prepareArgs);
        default:
          throw new Error(`Unknown function name ${request.type}`);
      }
    }
    catch (error) {
      throw error;
    }
  }

  public async GetBalance(request: BalanceRequest): Promise<BalanceResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const token = request.network.tokens.find(t => t.symbol === request.asset);

    const balanceResult = await provider.getBalance(request.address, token.contract);

    const result: BalanceResponse =
    {
      amount: Number(balanceResult),
    }

    return result;
  }

  public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const lastBlockNumber = (await provider.getBlockNumber()).toNumber();
    const latestBlock = await provider.getBlock(lastBlockNumber);

    return {
      blockNumber: lastBlockNumber,
      blockHash: latestBlock.id,
    };
  }

  public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee> {
    try {
      const token = feeRequest.network.tokens.find(t => t.symbol === feeRequest.asset);

      const htlcContractAddress = token.contract
        ? feeRequest.network.htlcNativeContractAddress
        : feeRequest.network.htlcTokenContractAddress

      const requestData = JSON.parse(feeRequest.callData);

      const provider = new Provider(feeRequest.network.nodes[0].url);
      const contractInstance = new Contract(htlcContractAddress, abi, provider);
      const functionName = requestData.func.name;
      let transactionCost: TransactionCost;

      const b256: B256Address = token.contract;
      const address: Address = new Address(b256);
      const assetId: AssetId = address.toAssetId();

      if (functionName == "lock") {
        const amount = Number(utils.parseUnits(feeRequest.amount.toString(), token.decimals))
        transactionCost = await contractInstance.functions[functionName](...requestData.args)
          .callParams({
            forward: [amount, assetId.bits],
          }).getTransactionCost();
      }
      else {

        transactionCost = await contractInstance.functions[functionName](...requestData.args).getTransactionCost();
      }

      const balanceResponse = await this.GetBalance({
        network: feeRequest.network,
        address: feeRequest.fromAddress,
        asset: feeRequest.asset
      });

      const fixedfeeData: FixedFeeData = {
        FeeInWei: transactionCost.maxFee.toString(),
      };

      if (balanceResponse.amount < Number(fixedfeeData.FeeInWei) + Number(feeRequest.amount)) {
        throw new Error(`Insufficient balance for transaction. Required: ${fixedfeeData.FeeInWei + feeRequest.amount}, Available: ${balanceResponse.amount}`);
      }

      const result: Fee = {
        Asset: feeRequest.network.nativeToken.symbol,
        FixedFeeData: fixedfeeData,
      }

      return result;
    }

    catch (error: any) {
      if (error?.message && (error.message.includes("Invalid Reward Timelock") || error.message.includes("No Future Timelock"))) {
        throw new Error;
      }
      throw error;
    }
  }

  public async ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {

    const timelock = DateTime.fromUnixSeconds(request.timelock).toTai64();
    const provider = new Provider(request.network.nodes[0].url);
    const signerAddress = Wallet.fromAddress(request.signerAddress, provider).address;

    const idBytes = new BigNumberCoder('u256').encode(request.commitId);
    const hashlockBytes = new B256Coder().encode(request.hashlock);
    const timelockBytes = new BigNumberCoder('u64').encode(bn(timelock));

    const rawData = concat([idBytes, hashlockBytes, timelockBytes]);
    const message = sha256(rawData);
    const messageHash = hashMessage(message);
    const recoveredAddress: Address = Signer.recoverAddress(messageHash, request.signature);
    const isValid = recoveredAddress.toString() === signerAddress.toString();

    return isValid;
  }

  public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

    const result = await TrackBlockEventsAsync(request.network, request.fromBlock, request.toBlock, request.walletAddresses);
    return result;
  }

  public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const transaction = await (await provider.getTransactionResponse(request.transactionHash)).getTransactionSummary();
    const transactionStatus = mapFuelStatusToInternal(transaction.status)

    if (transactionStatus == TransactionStatus.Failed) {
      throw new TransactionFailedException(`Transaction ${request.transactionHash} failed on network ${request.network.name}`);
    }

    const latestblock = await provider.getBlockNumber();
    const txBlock = await provider.getBlock(transaction.blockId);
    const confirmations = latestblock.toNumber() - txBlock.height.toNumber();

    const transactionResponse: TransactionResponse = {
      NetworkName: request.network.name,
      TransactionHash: request.transactionHash,
      Confirmations: confirmations,
      Timestamp: transaction.date,
      FeeAmount: Number(transaction.fee),
      FeeAsset: request.network.nativeToken.symbol,
      Status: transactionStatus,
    }

    return transactionResponse;
  }

  public async PublishTransaction(request: FuelPublishTransactionRequest): Promise<string> {

    const privateKey = await new PrivateKeyRepository().getAsync(request.fromAddress);
    const provider = new Provider(request.network.nodes[0].url);
    const wallet = Wallet.fromPrivateKey(privateKey, provider);
    const requestData = JSON.parse(request.callData);

    const isTxnTypeScript = isTransactionTypeScript(JSON.parse(request.callData));

    if (!isTxnTypeScript) {
      throw new Error("Transaction is not of type Script");
    }

    const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));

    const { coins } = await wallet.getCoins(requestData.forward.assetId);

    for (const coin of coins) {
      txRequest.addCoinInput(coin);
    }

    const estimatedDependencies = await wallet.provider.estimateTxDependencies(txRequest);

    txRequest.maxFee = bn(estimatedDependencies.dryRunStatus.totalFee);
    txRequest.gasLimit = bn(estimatedDependencies.dryRunStatus.totalGas);

    txRequest.addAccountWitnesses(wallet);

    const transactionId = await wallet.sendTransaction(txRequest);

    const result = transactionId.id;

    return result;
  }
}

export function formatAddress(address: string): string {
  return address.toLowerCase();
}