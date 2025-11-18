import { BaseSignTransactionRequestModel } from "./TreasurySignTransactionRequestModel";

export interface AztecSignTransactionRequest extends BaseSignTransactionRequestModel {
    nodeUrl: string;
    tokenContract: string;
    contractAddress: string;
}