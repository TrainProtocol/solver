import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { FuelComposeTransactionRequest } from "../Models/FuelComposeTransactionRequest";
import { FuelSignTransactionRequestModel } from "./Models/FuelSignTransactionModel";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";

export interface IFuelBlockchainActivities extends IBlockchainActivities {
    getTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

    buildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;

    publishTransaction(request: FuelPublishTransactionRequest): Promise<string>;

    composeRawTransaction(request: FuelComposeTransactionRequest): Promise<string>;

    signTransaction(request: FuelSignTransactionRequestModel): Promise<string>;

    getNextNonce(request: NextNonceRequest): Promise<number>;

    checkCurrentNonce(request: CurrentNonceRequest): Promise<void>;

    updateCurrentNonce(request: CurrentNonceRequest) : Promise<void>;
}