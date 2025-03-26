import { TransactionType } from "../TransactionTypes/TransactionType";

export interface TransactionBuilderRequest {
    TransactionType: TransactionType;
    NetworkName: string;
    Args: string;
}