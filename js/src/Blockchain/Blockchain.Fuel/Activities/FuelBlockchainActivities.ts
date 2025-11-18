import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { BigNumberCoder, Provider, Wallet, Signer, sha256, DateTime, bn, hashMessage, B256Coder, concat, Address, isTransactionTypeScript, transactionRequestify, ScriptTransactionRequest, AssetId } from "fuels";
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { IFuelBlockchainActivities } from "./IFuelBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import TrackBlockEventsAsync from "./Helper/FuelEventTracker";
import { createAddLockSigCallData, createRefundCallData, createLockCallData, createRedeemCallData, createCommitCallData, createTransferCallData } from "./Helper/FuelTransactionBuilder";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { mapFuelStatusToInternal } from "./Helper/FuelTransactionStatusMapper";
import { FuelComposeTransactionRequest } from "../Models/FuelComposeTransactionRequest";
import { FuelSufficientBalanceRequest } from "../Models/FuelSufficientBalanceRequest";
import { InvalidTimelockException } from "../../Blockchain.Abstraction/Exceptions/InvalidTimelockException";
import { inject, injectable } from "tsyringe";
import Redis from "ioredis";
import Redlock from "redlock";
import { TreasuryClient } from "../../Blockchain.Abstraction/Infrastructure/TreasuryClient/treasuryClient";
import { SignTransactionRequest } from "./Models/FuelSignTransactionModel";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { buildLockKey as buildLockKey, buildCurrentNonceKey, buildNextNonceKey } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/RedisHelper";
import { TimeSpan } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/TimeSpanConverter";

@injectable()
export class FuelBlockchainActivities implements IFuelBlockchainActivities {
    constructor(
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock
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
                case TransactionType.Transfer:
                    return createTransferCallData(request.network, request.prepareArgs);
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
            amount: balanceResult.toString(),
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

    public async PublishTransaction(request: FuelPublishTransactionRequest): Promise<string> {
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

            throw new TransactionFailedException(`Transaction failed message: ${error.message}`);
        }
    }

    public async ComposeRawTransaction(request: FuelComposeTransactionRequest): Promise<string> {
        try {
            const provider = new Provider(request.network.nodes[0].url);
            const wallet = Wallet.fromAddress(request.fromAddress, provider);
            const requestData = JSON.parse(request.callData);
            const token = request.network.tokens.find(t => t.symbol === request.callDataAsset);
            const nativeToken = request.network.nativeToken;

            const isTxnTypeScript = isTransactionTypeScript(JSON.parse(request.callData));

            if (!isTxnTypeScript) {
                throw new Error("Transaction is not of type Script");
            }

            const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));
            const isNative = token.symbol === nativeToken.symbol;

            const coinInputs = txRequest.getCoinInputs();

            if (isNative && coinInputs.length === 0) {

                const balance = await wallet.getCoins(await provider.getBaseAssetId());

                for (const coin of balance.coins) {
                    txRequest.addCoinInput(coin);
                }
            }
            else if (!isNative && coinInputs.length === 0) {
                const nativeBalance = await wallet.getCoins(await provider.getBaseAssetId());

                for (const coin of nativeBalance.coins) {
                    txRequest.addCoinInput(coin);
                }

                const assetId: AssetId = new Address(token.contract).toAssetId();

                const tokenBalance = await wallet.getCoins(assetId.bits);

                for (const coin of tokenBalance.coins) {
                    txRequest.addCoinInput(coin);
                }
            }

            const estimatedDependencies = await wallet.provider.getTransactionCost(txRequest);

            txRequest.maxFee = estimatedDependencies.maxFee;

            txRequest.gasLimit = estimatedDependencies.gasUsed;

            await this.EnsureSufficientBalance(
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
        catch (error) {
            if (error.metadata.logs.includes("Not Future Timelock")) {
                throw new InvalidTimelockException(`Transaction has an invalid timelock`);
            }

            throw error;
        }
    }

    private async EnsureSufficientBalance(request: FuelSufficientBalanceRequest): Promise<void> {

        const nativeAssetId = await request.wallet.provider.getBaseAssetId();
        const coinInputs = request.rawData.getCoinInputs();

        const nativeBalance = coinInputs
            .filter(coin => coin.assetId === nativeAssetId)
            .reduce((sum, coin) => sum + Number(coin.amount), 0)

        const maxFee = Number(request.rawData.maxFee);

        const isNative = request.callDataAsset === request.network.nativeToken.symbol;

        if (isNative) {

            if (nativeBalance < maxFee + Number(request.callDataAmount)) {
                throw new Error(`Insufficient balance for ${request.network.nativeToken.symbol}`);
            }
        }
        else {

            const token = request.network.tokens.find(t => t.symbol === request.callDataAsset);
            if (!token) {
                throw new Error(`Token ${request.callDataAsset} not found in network`);
            }

            const topkenAssetId = new Address(token.contract).toAssetId().bits;

            const tokenAssetBalance = coinInputs
                .filter(coin => coin.assetId === topkenAssetId)
                .reduce((sum, coin) => sum + Number(coin.amount), 0);

            if (tokenAssetBalance < request.callDataAmount) {
                throw new Error(`Insufficient balance for ${request.callDataAsset}`);
            }

            if (nativeBalance < maxFee) {
                throw new Error(`Insufficient balance for ${request.network.nativeToken.symbol}`);
            }
        }
    }

    public async SignTransaction(request: SignTransactionRequest): Promise<string> {

        const treasuryClient = new TreasuryClient(request.signerAgentUrl);

        const response = await treasuryClient.signTransaction(request.networkType, request.signRequest);

        return response.signedTxn;
    }

    public async GetNextNonce(request: NextNonceRequest): Promise<number> {
        const lockKey = buildLockKey(request.network.name, request.address);

        const nextNonceKey = buildNextNonceKey(request.network.name, request.address);

        const lock = await this.lockFactory.acquire(
            [lockKey],
            TimeSpan.FromSeconds(25),
            {
                retryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
            });

        try {
            let pendingNonce: number = 0;

            const cached = await this.redis.get(nextNonceKey);

            if (cached !== null) {
                pendingNonce = Number(cached);
            }

            await this.redis.set(nextNonceKey, (pendingNonce + 1).toString(), "EX", TimeSpan.FromDays(7));

            return pendingNonce

        } finally {
            await lock.release().catch(() => { });
        }
    }

    public async CheckCurrentNonce(request: CurrentNonceRequest): Promise<void> {
        const currentNonceKey = buildCurrentNonceKey(request.network.name, request.address);

        const cached = await this.redis.get(currentNonceKey);

        let addressCurrentNonce: number = 0;

        if (cached !== null) {
            addressCurrentNonce = Number(cached);
        }

        if (addressCurrentNonce !== request.currentNonce) {
            throw new Error(`Current nonce ${addressCurrentNonce} transaction nonce ${request.currentNonce}`)
        }
    }


    public async UpdateCurrentNonce(request: CurrentNonceRequest): Promise<void> {
        const lockKey = buildLockKey(request.network.name, request.address);

        const currentNonceKey = buildCurrentNonceKey(request.network.name, request.address);

        const lock = await this.lockFactory.acquire(
            [lockKey],
            TimeSpan.FromSeconds(25),
            {
                retryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
            });

        try {
            await this.redis.set(currentNonceKey, (request.currentNonce + 1).toString(), "EX", TimeSpan.FromDays(7));
        }
        finally {
            await lock.release().catch(() => { });
        }
    }
}

export function FormatAddress(address: string): string {
    return address.toLowerCase();
}
