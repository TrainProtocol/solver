import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { IAztecBlockchainActivities } from "./IAztecBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import TrackBlockEventsAsync from "./Helper/AztecEventTracker";
import { createRefundCallData, createLockCallData, createRedeemCallData, createCommitCallData } from "./Helper/AztecTransactionBuilder";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { Tx, TxHash } from "@aztec/aztec.js/tx";
import { createAztecNodeClient } from '@aztec/aztec.js/node';
import { mapAztecStatusToInternal } from "./Helper/AztecTransactionStatusMapper";
import { AztecPublishTransactionRequest } from "../Models/AztecPublishTransactionRequest";
import { TreasuryClient } from "../../Blockchain.Abstraction/Infrastructure/TreasuryClient/treasuryClient";
import { AztecSignTransactionRequestModel } from "./Models/AztecSignTransactionModel";
import { buildLockKey as buildLockKey, buildCurrentNonceKey, buildNextNonceKey } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/RedisHelper";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";
import { inject, injectable } from "tsyringe";
import Redis from "ioredis";
import Redlock from "redlock";
import { TimeSpan } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/TimeSpanConverter";
import { TransactionNotComfirmedException } from "../../Blockchain.Abstraction/Exceptions/TransactionNotComfirmedException";

@injectable()
export class AztecBlockchainActivities implements IAztecBlockchainActivities {
    constructor(
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock,
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
        return { amount: 1000000000000 };
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);
        const lastBlockNumber = await provider.getBlockNumber();
        const blockHash = await (await provider.getBlock(lastBlockNumber)).hash();

        return {
            blockNumber: lastBlockNumber,
            blockHash: blockHash.toString()
        };
    }

    public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

        const result = await TrackBlockEventsAsync(request.network, request.fromBlock, request.toBlock, request.walletAddresses);
        return result;
    }

    public async getTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);
        const transaction = await provider.getTxReceipt(TxHash.fromString(request.transactionHash));
        const transactionStatus = mapAztecStatusToInternal(transaction.status)

        if (transactionStatus == TransactionStatus.Initiated) {
            throw new TransactionNotComfirmedException(`Transaction ${request.transactionHash} is still pending on network ${request.network.name}`);
        }

        if (transactionStatus == TransactionStatus.Failed) {
            throw new TransactionFailedException(`Transaction ${request.transactionHash} failed on network ${request.network.name}`);
        }

        const transactionBlock = await provider.getBlock(transaction.blockNumber);
        const latestblock = await provider.getProvenBlockNumber();
        const timestamp = transactionBlock.header.globalVariables.timestamp;
        const confirmations = latestblock - transactionBlock.number;

        const transactionResponse: TransactionResponse = {
            decimals: request.network.nativeToken.decimals,
            networkName: request.network.name,
            transactionHash: request.transactionHash,
            confirmations: confirmations,
            timestamp: new Date(Number(timestamp) * 1000),
            feeAmount: "0",
            feeAsset: request.network.nativeToken.symbol,
            feeDecimals: request.network.nativeToken.decimals,
            status: transactionStatus,
        }

        return transactionResponse;
    }

    public async signTransaction(request: AztecSignTransactionRequestModel): Promise<string> {

        const treasuryClient = new TreasuryClient(request.signerAgentUrl);

        const response = await treasuryClient.signTransaction(request.networkType, request.signRequest);

        return response.signedTxn;
    }

    public async publishTransaction(request: AztecPublishTransactionRequest): Promise<string> {

        const parsed = JSON.parse(request.signedTx);
        const buf = Buffer.from(parsed.signedTx, "hex");
        const signedTx = Tx.fromBuffer(buf);

        const provider = createAztecNodeClient(request.network.nodes[0].url);

        await provider.sendTx(signedTx);
        const txHash = signedTx.getTxHash().toString();

        return txHash;
    }

    public async getNextNonce(request: NextNonceRequest): Promise<number> {
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

    public async checkCurrentNonce(request: CurrentNonceRequest): Promise<void> {
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

    public async updateCurrentNonce(request: CurrentNonceRequest): Promise<void> {
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

    ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        throw new Error("Method not implemented.");
    }
}
