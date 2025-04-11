import { BaseRequest } from "./BaseRequest";

export interface GetBatchTransactionRequest extends BaseRequest {
    TransactionHashes: string[];
}