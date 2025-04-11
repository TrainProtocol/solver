import { TransactionType } from "../TransacitonModels/TransactionType";

export interface TransactionBuilderRequest {
    TransactionType: TransactionType;
    NetworkName: string;
    Args: string;
}