import { BaseSignTransactionRequestModel } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models/TreasurySignTransactionRequestModel";

export interface FuelSignTransactionRequestModel extends BaseSignTransactionRequestModel {
    nodeUrl: string;
}