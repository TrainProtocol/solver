import { BaseRequest } from "../BaseRequest";
import { TransactionType } from "./TransactionType";

export interface TransactionRequest extends BaseRequest {
    prepareArgs: string;
    type: TransactionType;
    fromAddress: string;
    swapId?: number;
}