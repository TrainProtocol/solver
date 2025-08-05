import { BaseSignTransactionRequestModel } from "./TreasurySignTransactionRequestModel";

export interface FuelSignTransactionRequest  extends BaseSignTransactionRequestModel {
    nodeUrl: string;
}