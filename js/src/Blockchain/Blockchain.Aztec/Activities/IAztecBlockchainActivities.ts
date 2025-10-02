import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { AztecPublishTransactionRequest } from "../Models/AztecPublishTransactionRequest";
import { AztecSignTransactionRequestModel } from "./Models/AztecSignTransactionModel";

export interface IAztecBlockchainActivities extends IBlockchainActivities {

    getTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

    publishTransaction(request: AztecPublishTransactionRequest): Promise<string>;

    signTransaction(request: AztecSignTransactionRequestModel): Promise<string>;

    getNextNonce(request: NextNonceRequest): Promise<number>;

    checkCurrentNonce(request: CurrentNonceRequest): Promise<void>;

    updateCurrentNonce(request: CurrentNonceRequest): Promise<void>;
}