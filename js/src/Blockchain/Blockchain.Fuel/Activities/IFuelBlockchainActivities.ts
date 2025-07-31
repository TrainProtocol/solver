import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { FuelComposeTransactionRequest } from "../Models/FuelComposeTransactionRequest";
import { FuelSufficientBalanceRequest } from "../Models/FuelSufficientBalanceRequest";

export interface IFuelBlockchainActivities extends IBlockchainActivities {
    getTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

    buildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;

    publishTransaction(request: FuelPublishTransactionRequest): Promise<string>;

    composeRawTransaction(request: FuelComposeTransactionRequest): Promise<string>;

    ensureSufficientBalance(request: FuelSufficientBalanceRequest): Promise<void>;
}