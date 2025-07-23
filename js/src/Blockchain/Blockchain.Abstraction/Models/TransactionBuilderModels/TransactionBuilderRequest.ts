import { TransactionType } from "../TransacitonModels/TransactionType";

export interface TransactionBuilderRequest {
    prepareArgs: string;
    type: TransactionType;
    fromAddress: string;
    swapId?: number;
}