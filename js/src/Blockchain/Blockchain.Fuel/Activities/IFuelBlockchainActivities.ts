import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";

export interface IFuelBlockchainActivities extends IBlockchainActivities {

    EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee>;

    GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

    BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;

    PublishTransaction(request: FuelPublishTransactionRequest): Promise<string>;
}