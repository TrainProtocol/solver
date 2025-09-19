import { BaseSignTransactionRequestModel } from "./TreasurySignTransactionRequestModel";

export interface FuelSignTransactionRequestModel  extends BaseSignTransactionRequestModel {
    nodeUrl: string;
}