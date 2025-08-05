import { BaseRequest } from "../BaseRequest";

export interface GetTransactionRequest extends BaseRequest {
    transactionHash: string;
}