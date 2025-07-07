import { TransactionType } from "../TransacitonModels/TransactionType";

export interface TransactionBuilderRequest {
    Type: TransactionType;
    NetworkName: string;
    Args: string;
}