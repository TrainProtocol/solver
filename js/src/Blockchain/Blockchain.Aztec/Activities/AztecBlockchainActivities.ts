import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
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
import { AztecPublishTransactionRequest } from "../Models/AztecPublishTransactionRequest";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { createAztecNodeClient, TxHash } from "@aztec/aztec.js";
import { mapAztecStatusToInternal } from "./Helper/AztecTransactionStatusMapper";

export class AztecBlockchainActivities implements IAztecBlockchainActivities {

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
        let result: BalanceResponse = { amount: 1000000000000 }
        return result;
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);
        const lastBlockNumber = await provider.getProvenBlockNumber();
        const blockHash = await (await provider.getBlock(lastBlockNumber)).hash();

        return {
            blockNumber: lastBlockNumber,
            blockHash: blockHash.toString()
        };
    }

    public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee> {
        return null;
    }

    public async ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {

        return true;
    }

    public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

        const result = await TrackBlockEventsAsync(request.network, request.fromBlock, request.toBlock, request.walletAddresses);
        return result;
    }

    public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);

        const transaction = await provider.getTxReceipt(TxHash.fromString(request.transactionHash));

        if (transaction.status != 'success') {
            throw new TransactionFailedException(`Transaction ${request.transactionHash} failed on network ${request.network.name}`);
        }

        const transactionBlock = await provider.getBlock(transaction.blockNumber);
        const latestblock = await provider.getProvenBlockNumber();
        const timestamp = transactionBlock.header.globalVariables.timestamp.toString();
        const confirmations = latestblock - transactionBlock.number;
        const transactionStatus = mapAztecStatusToInternal(transaction.status)

        if (transactionStatus == TransactionStatus.Failed) {
            throw new TransactionFailedException(`Transaction ${request.transactionHash} failed on network ${request.network.name}`);
        }

        const transactionResponse: TransactionResponse = {
            NetworkName: request.network.name,
            TransactionHash: request.transactionHash,
            Confirmations: confirmations,
            Timestamp: new Date(timestamp),//need to test
            FeeAmount: Number(transaction.transactionFee),
            FeeAsset: request.network.nativeToken.symbol,
            Status: transactionStatus,
        }

        return transactionResponse;
    }

    public async PublishTransaction(request: AztecPublishTransactionRequest): Promise<string> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);

        await provider.sendTx(request.tx);

        return '';
    }
}
//need to investigate
export function formatAddress(address: string): string {
    return address.toLowerCase();
}