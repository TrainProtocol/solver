import { NetworkType } from "../../../Data/Entities/Networks";
import { BaseRequest } from "../BaseRequest";
import { TransactionType } from "./TransactionType";

export interface TransactionRequest extends BaseRequest {
    PrepareArgs: string;
    TransactionType: TransactionType;
    NetworkType: NetworkType;
    FromAddress: string;
    SwapId: string;
}