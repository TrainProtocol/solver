import { BaseSignTransactionRequestModel } from "./TreasurySignTransactionRequestModel";

export interface StarknetSignTransactionRequestModel extends BaseSignTransactionRequestModel { 
    signerInvocationDetails: string
}