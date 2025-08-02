import { FuelSignTransactionRequestModel } from "./FuelSignTransactionModel";

export interface FuelSignTransactionRequest {
    networkType: string;
    signRequest : FuelSignTransactionRequestModel;
}