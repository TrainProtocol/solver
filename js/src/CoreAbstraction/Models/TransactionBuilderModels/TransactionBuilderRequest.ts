import { TransactionType } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionType";

export interface TransactionBuilderRequest {
    TransactionType: TransactionType;
    NetworkName: string;
    Args: string;
}