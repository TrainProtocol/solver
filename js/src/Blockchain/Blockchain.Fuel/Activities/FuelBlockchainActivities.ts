import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { BigNumberCoder, Provider, Wallet, Signer, sha256, DateTime, bn, hashMessage, B256Coder, concat, Address, isTransactionTypeScript, transactionRequestify, ScriptTransactionRequest} from "fuels";
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { IFuelBlockchainActivities } from "./IFuelBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import TrackBlockEventsAsync from "./Helper/FuelEventTracker";
import { createAddLockSigCallData, createRefundCallData, createLockCallData, createRedeemCallData, createCommitCallData } from "./Helper/FuelTransactionBuilder";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { mapFuelStatusToInternal } from "./Helper/FuelTransactionStatusMapper";
import { FuelComposeTransactionRequest } from "../Models/FuelComposeTransactionRequest";
import { FuelSufficientBalanceRequest } from "../Models/FuelSufficientBalanceRequest";
import { InvalidTimelockException } from "../../Blockchain.Abstraction/Exceptions/InvalidTimelockException";

export class FuelBlockchainActivities implements IFuelBlockchainActivities {

  public async buildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
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

  public async getBalance(request: BalanceRequest): Promise<BalanceResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const token = request.network.tokens.find(t => t.symbol === request.asset);

    const balanceResult = await provider.getBalance(request.address, token.contract);

    const result: BalanceResponse =
    {
      amount: Number(balanceResult),
    }

    return result;
  }

  public async getLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const lastBlockNumber = (await provider.getBlockNumber()).toNumber();
    const latestBlock = await provider.getBlock(lastBlockNumber);

    return {
      blockNumber: lastBlockNumber,
      blockHash: latestBlock.id,
    };
  }

  public async validateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {

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

  public async getEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

    const result = await TrackBlockEventsAsync(request.network, request.fromBlock, request.toBlock, request.walletAddresses);

    return result;
  }

  public async getTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

    const provider = new Provider(request.network.nodes[0].url);
    const transaction = await provider.getTransactionResponse(request.transactionHash);

    const transactionSummary = await transaction.getTransactionSummary();
    const transactionStatus = mapFuelStatusToInternal(transactionSummary.status);

    if (transactionStatus == TransactionStatus.Initiated) {
      throw new Error(`Transaction ${request.transactionHash} is still pending on network ${request.network.name}`);
    }

    const latestblock = await provider.getBlockNumber();
    const txBlock = await provider.getBlock(transactionSummary.blockId);
    const confirmations = latestblock.toNumber() - txBlock.height.toNumber();

    const transactionResponse: TransactionResponse = {
      decimals: request.network.nativeToken.decimals,
      feeDecimals: request.network.nativeToken.decimals,
      networkName: request.network.name,
      transactionHash: request.transactionHash,
      confirmations: confirmations,
      timestamp: transactionSummary.date,
      feeAmount: Number(transactionSummary.fee).toString(),
      feeAsset: request.network.nativeToken.symbol,
      status: transactionStatus,
    }

    return transactionResponse;
  }

  public async publishTransaction(request: FuelPublishTransactionRequest): Promise<string> {
    let result: string;

    try {
      const provider = new Provider(request.network.nodes[0].url);
      const requestData = JSON.parse(request.signedRawData);

      const isTxnTypeScript = isTransactionTypeScript(JSON.parse(request.signedRawData));

      if (!isTxnTypeScript) {
        throw new Error("Transaction is not of type Script");
      }

      const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));

      const { id, waitForResult } = await provider.sendTransaction(txRequest);

      result = id;
      await waitForResult();

      return result;
    }
    catch (error) {
      if (error.metadata.logs.includes("Not Future Timelock")) {
        throw new InvalidTimelockException(`Transaction has an invalid timelock`);
      }
      if (error.metadata.logs.includes("Already Claimed")) {
        return result;
      }

      return error;
    }
  }

  public async composeRawTransaction(request: FuelComposeTransactionRequest): Promise<string> {

    const provider = new Provider(request.network.nodes[0].url);
    const wallet = Wallet.fromAddress(request.fromAddress, provider);
    const requestData = JSON.parse(request.callData);

    const isTxnTypeScript = isTransactionTypeScript(JSON.parse(request.callData));

    if (!isTxnTypeScript) {
      throw new Error("Transaction is not of type Script");
    }

    const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));

    const balance = await wallet.getCoins(await provider.getBaseAssetId());

    for (const coin of balance.coins) {
      txRequest.addCoinInput(coin);
    }

    const estimatedDependencies = await wallet.provider.estimateTxDependencies(txRequest);

    txRequest.maxFee = bn(estimatedDependencies.dryRunStatus.totalFee).mul(7);
    txRequest.gasLimit = bn(estimatedDependencies.dryRunStatus.totalGas).mul(2);

    wallet.simulateTransaction(txRequest);

    await this.ensureSufficientBalance(
      {
        network: request.network,
        rawData: txRequest,
        wallet: wallet,
        callDataAsset: request.callDataAsset,
        callDataAmount: request.callDataAmount
      }
    )

    return JSON.stringify(txRequest);
  }

  private async ensureSufficientBalance(request: FuelSufficientBalanceRequest): Promise<void> {

    const nativeAssetId = await request.wallet.provider.getBaseAssetId();
    const coinInputs = request.rawData.getCoinInputs();

    const nativeBalance = Number(
      coinInputs.find(coin => coin.assetId === nativeAssetId)?.amount || 0
    );

    const maxFee = Number(request.rawData.maxFee);

    const isNative = request.callDataAsset === request.network.nativeToken.symbol;

    if (isNative) {
      if (nativeBalance < maxFee + request.callDataAmount) {
        throw new Error(`Insufficient balance for ${request.network.nativeToken.symbol}`);
      }
      return;
    }

    const token = request.network.tokens.find(t => t.symbol === request.callDataAsset);
    if (!token) {
      throw new Error(`Token ${request.callDataAsset} not found in network`);
    }

    const assetId = new Address(token.contract).toAssetId().bits;
    const assetBalance = Number(
      coinInputs.find(coin => coin.assetId === assetId)?.amount || 0
    );

    if (assetBalance < request.callDataAmount) {
      throw new Error(`Insufficient balance for ${request.callDataAsset}`);
    }

    if (nativeBalance < maxFee) {
      throw new Error(`Insufficient balance for ${request.network.nativeToken.symbol}`);
    }
  }
}

export function formatAddress(address: string): string {
  return address.toLowerCase();
}