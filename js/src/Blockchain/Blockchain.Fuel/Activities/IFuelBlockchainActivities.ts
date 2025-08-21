import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { FuelComposeTransactionRequest } from "../Models/FuelComposeTransactionRequest";
import { FuelSignTransactionRequestModel } from "./Models/FuelSignTransactionModel";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";

export interface IFuelBlockchainActivities extends IBlockchainActivities {

    GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

    PublishTransaction(request: FuelPublishTransactionRequest): Promise<string>;

    ComposeRawTransaction(request: FuelComposeTransactionRequest): Promise<string>;

    SignTransaction(request: FuelSignTransactionRequestModel): Promise<string>;

    GetNextNonce(request: NextNonceRequest): Promise<number>;

    CheckCurrentNonce(request: CurrentNonceRequest): Promise<void>;

    UpdateCurrentNonce(request: CurrentNonceRequest) : Promise<void>;
}