import { BaseRequest } from "../BaseRequest";
import { TransactionType } from "../TransacitonModels/TransactionType";

export interface TransactionBuilderRequest extends BaseRequest {
    prepareArgs: string;
    type: TransactionType;
    fromAddress: string;
    swapId?: number;
}