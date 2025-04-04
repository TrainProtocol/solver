import { BaseRequest } from "../BaseRequest";
import { NetworkType } from "../NetworkModels/NetworkType";
import { TransactionType } from "./TransactionType";

export interface TransactionRequest extends BaseRequest {
    PrepareArgs: string;
    TransactionType: TransactionType;
    NetworkType: NetworkType;
    FromAddress: string;
    SwapId: string;
}