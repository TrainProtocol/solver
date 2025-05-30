import { ContractType } from "../../../Data/Entities/Contracts";
import { AccountType } from "../../../Data/Entities/ManagedAccounts";
import { NodeType } from "../../../Data/Entities/Nodes";
import { SolverContext } from "../../../Data/SolverContext";
import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee, FixedFeeData, LegacyFeeData } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { AssetFuel, Contract, BigNumberCoder, getAssetFuel, Provider, rawAssets, ReceiptType, TransactionCost, Wallet, hexlify, arrayify, Signer, sha256, DateTime, bn, hashMessage, B256Coder, concat, Address, B256Address, AssetId, formatUnits } from "fuels";
import abi from './ABIs/ERC20.json';
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { inject, injectable } from "tsyringe";
import { IFuelBlockchainActivities } from "./IFuelBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { utils } from "ethers";
import TrackBlockEventsAsync from "./Helper/FuelEventTracker";
import { CreateAddLockSigCallData, CreateRefundCallData, CreateLockCallData, CreateRedeemCallData } from "./Helper/FuelTransactionBuilder";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { PrivateKeyRepository } from "../../Blockchain.Abstraction/Models/WalletsModels/PrivateKeyRepository";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { mapFuelStatusToInternal } from "./Helper/FuelTransactionStatusMapper";

@injectable()
export class FuelBlockchainActivities implements IFuelBlockchainActivities {
  constructor(
    @inject(SolverContext) private dbContext: SolverContext
  ) { }

  public async BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
    try {

      const network = await this.dbContext.Networks
        .createQueryBuilder("network")
        .leftJoinAndSelect("network.nodes", "n")
        .leftJoinAndSelect("network.tokens", "t")
        .leftJoinAndSelect("network.contracts", "c")
        .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
        .getOneOrFail();

      switch (request.TransactionType) {
        case TransactionType.HTLCLock:
          return CreateLockCallData(network, request.Args);
        case TransactionType.HTLCRedeem:
          return CreateRedeemCallData(network, request.Args);
        case TransactionType.HTLCRefund:
          return CreateRefundCallData(network, request.Args);
        case TransactionType.HTLCAddLockSig:
          return CreateAddLockSigCallData(network, request.Args);
        default:
          throw new Error(`Unknown function name ${request.TransactionType}`);
      }
    }
    catch (error) {
      throw error;
    }
  }

  public async GetBalance(request: BalanceRequest): Promise<BalanceResponse> {
    try {
      const network = await this.dbContext.Networks
        .createQueryBuilder("network")
        .leftJoinAndSelect("network.nodes", "n")
        .leftJoinAndSelect("network.tokens", "t")
        .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
        .getOneOrFail();

      const node = network.nodes.find(n => n.type === NodeType.Primary);
      if (!node) {
        throw new Error(`Primary node not found for network ${request.NetworkName}`);
      }

      const token = network.tokens.find(t => t.asset === request.Asset);
      if (!token) {
        throw new Error(`Token not found for network ${request.NetworkName} and asset ${request.Asset}`);
      }

      const provider = new Provider(node.url);
      const balanceResult = await provider.getBalance(request.Address, token.tokenContract);
      var balanceInWei = balanceResult.toString();

      let result: BalanceResponse =
      {
        Amount: Number(utils.formatUnits(balanceInWei, token.decimals)),
        AmountInWei: balanceInWei,
        Decimals: token.decimals
      }

      return result;
    }
    catch (error) {
      throw error;
    }
  }

  public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {
    try {
      const network = await this.dbContext.Networks
        .createQueryBuilder("network")
        .leftJoinAndSelect("network.nodes", "n")
        .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
        .getOne();

      if (!network) {
        throw new Error(`Network ${request.NetworkName} not found`);
      }

      const node = network.nodes.find((n) => n.type === NodeType.Primary);
      if (!node) {
        throw new Error(
          `Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`
        );
      }

      const provider = new Provider(node.url);
      const lastBlockNumber = (await provider.getBlockNumber()).toNumber();
      const latestBlock = await provider.getBlock(lastBlockNumber);

      return {
        BlockNumber: lastBlockNumber,
        BlockHash: latestBlock.id,
      };
    }
    catch (error) {
      throw error;
    }
  }

  public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee> {
    try {
      const network = await this.dbContext.Networks
        .createQueryBuilder("network")
        .leftJoinAndSelect("network.nodes", "n")
        .leftJoinAndSelect("network.contracts", "c")
        .leftJoinAndSelect("network.tokens", "t")
        .where("UPPER(network.name) = UPPER(:nName)", { nName: feeRequest.NetworkName })
        .getOneOrFail();

      const node = network.nodes.find(n => n.type === NodeType.Primary);

      if (!node) {
        throw new Error(`Primary node not found for network ${feeRequest.NetworkName}`);
      }
      const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress);
      const requestData = JSON.parse(feeRequest.CallData);

      const token = network.tokens.find(t => t.asset === feeRequest.Asset);
      if (!token) {
        throw new Error(`Token not found for network ${network.name} and asset ${feeRequest.Asset}`);
      }

      const provider = new Provider(node.url);
      const contractInstance = new Contract(htlcContractAddress.address, abi, provider);
      const functionName = requestData.func.name;
      let transactionCost: TransactionCost;

      const b256: B256Address = token.tokenContract;
      const address: Address = Address.fromB256(b256);
      const assetId: AssetId = address.toAssetId();

      if (functionName == "lock") {
        const amount = Number(utils.parseUnits(feeRequest.Amount.toString(), token.decimals))
        transactionCost = await contractInstance.functions[functionName](...requestData.args)
          .callParams({
            forward: [amount, assetId.bits],
          }).getTransactionCost();
      }
      else {

        transactionCost = await contractInstance.functions[functionName](...requestData.args).getTransactionCost();
      }

      const fixedfeeData: FixedFeeData = {
        FeeInWei: transactionCost.maxFee.toString(),
      };

      const legacyFeeData: LegacyFeeData = {
        GasLimit: transactionCost.gasUsed.toString(),
        GasPriceInWei: transactionCost.gasPrice.toString(),
        L1FeeInWei: null
      };

      const feeToken = network.tokens.find(t => t.isNative === true);
      const result: Fee = {
        Asset: feeToken.asset,
        Decimals: feeToken.decimals,
        FixedFeeData: fixedfeeData,
        LegacyFeeData: legacyFeeData,
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

    const network = await this.dbContext.Networks
      .createQueryBuilder("network")
      .leftJoinAndSelect("network.nodes", "n")
      .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
      .getOne();

    if (!network) {
      throw new Error(`Network ${request.NetworkName} not found`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
      throw new Error(`Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`);
    }

    const timelock = DateTime.fromUnixSeconds(request.Timelock).toTai64();
    const provider = new Provider(node.url);
    const signerAddress = Wallet.fromAddress(request.SignerAddress, provider).address;

    const idBytes = new BigNumberCoder('u256').encode(request.Id);
    const hashlockBytes = new B256Coder().encode(request.Hashlock);
    const timelockBytes = new BigNumberCoder('u64').encode(bn(timelock));

    const rawData = concat([idBytes, hashlockBytes, timelockBytes]);
    const message = sha256(rawData);
    const messageHash = hashMessage(message);
    const recoveredAddress: Address = Signer.recoverAddress(messageHash, request.Signature);
    const isValid = recoveredAddress.toString() === signerAddress.toString();

    return isValid;
  }

  public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

    const network = await this.dbContext.Networks
      .createQueryBuilder("network")
      .leftJoinAndSelect("network.nodes", "n")
      .leftJoinAndSelect("network.contracts", "c")
      .leftJoinAndSelect("network.managedAccounts", "ma")
      .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
      .getOne();

    if (!network) {
      throw new Error(`Network ${request.NetworkName} not found`);
    }

    const node = network.nodes.find(n => n.type === NodeType.Primary);
    if (!node) {
      throw new Error(`Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`);
    }
    
    const solverAddress = network.managedAccounts.find(m => m.type === AccountType.Primary)?.address;
    const htlcAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)?.address;

    const tokens = await this.dbContext.Tokens
      .createQueryBuilder("currencies")
      .leftJoinAndSelect("currencies.network", "n")
      .getMany();

    const result = await TrackBlockEventsAsync(network.name, node.url, request.FromBlock, request.ToBlock, htlcAddress, solverAddress, tokens);

    return result;
  }

  public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

    let transactionResponse: TransactionResponse;
    const network = await this.dbContext.Networks
      .createQueryBuilder("network")
      .leftJoinAndSelect("network.tokens", "t")
      .leftJoinAndSelect("network.nodes", "n")
      .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
      .getOne();

    if (!network) {
      throw new Error(`Network ${request.NetworkName} not found`);
    }

    const node = network.nodes.find((n) => n.type === NodeType.Primary);
    if (!node) {
      throw new Error(
        `Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`
      );
    }

    const nativeToken = network.tokens.find(t => t.isNative === true);
    const provider = new Provider(node.url);
    const transaction = await (await provider.getTransactionResponse(request.TransactionHash)).getTransactionSummary();
    const transactionStatus = mapFuelStatusToInternal(transaction.status)

    if (transactionStatus == TransactionStatus.Failed) {
      throw new TransactionFailedException(`Transaction ${request.TransactionHash} failed on network ${network.name}`);
    }

    const latestblock = await provider.getBlockNumber();
    const txBlock = await provider.getBlock(transaction.blockId);
    const confirmations = latestblock.toNumber() - txBlock.height.toNumber();

    transactionResponse = {
      NetworkName: network.name,
      TransactionHash: request.TransactionHash,
      Confirmations: confirmations,
      Timestamp: transaction.date,
      FeeAmount: Number(utils.formatUnits(transaction.fee.toString(), nativeToken.decimals)),
      FeeAsset: nativeToken.asset,
      Status: transactionStatus,
    }

    return transactionResponse;
  }

  public async PublishTransaction(request: FuelPublishTransactionRequest): Promise<string> {

    let result: string;

    try {
      const network = await this.dbContext.Networks
        .createQueryBuilder("network")
        .leftJoinAndSelect("network.nodes", "n")
        .leftJoinAndSelect("network.contracts", "c")
        .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
        .getOneOrFail();

      const node = network.nodes.find(n => n.type === NodeType.Primary);

      if (!node) {
        throw new Error(`Primary node not found for network ${request.NetworkName}`);
      }

      const privateKey = await new PrivateKeyRepository().getAsync(request.FromAddress);
      const provider = new Provider(node.url);
      const wallet = Wallet.fromPrivateKey(privateKey, provider);
      const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)?.address;
      const contractInstance = new Contract(htlcContractAddress, abi, wallet);
      const requestData = JSON.parse(request.CallData);
      const selector = requestData.func.name;

      if (selector == "lock") {
        const { transactionId } = await contractInstance.functions[selector](...requestData.args)
          .callParams({
            forward: [request.Amount, requestData.forward.assetId],
           // gasLimit: request.Fee.LegacyFeeData.GasLimit,
          }).call();

        result = transactionId;
      }
      else {
        const { transactionId } = await contractInstance.functions[selector](...requestData.args)
         // .callParams({ gasLimit: request.Fee.LegacyFeeData.GasLimit })
          .call();
        result = transactionId;
      }
    }
    catch (error) {
      throw error;
    }

    return result;
  }
}

