import { NetworkType } from "../../../../Data/Entities/Networks";
import { BaseRequest } from "../BaseRequest";
import { TransactionType } from "./TransactionType";

export interface TransactionRequest extends BaseRequest {
    PrepareArgs: string;
    Type: TransactionType;
    NetworkType: NetworkType;
    FromAddress: string;
    SwapId: string;
}