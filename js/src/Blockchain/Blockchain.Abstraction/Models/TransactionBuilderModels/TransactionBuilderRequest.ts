import { BaseRequest } from "../BaseRequest";
import { TransactionType } from "../TransacitonModels/TransactionType";

export interface TransactionBuilderRequest extends BaseRequest {
    args: string;
    type: TransactionType;
    fromAddress: string;
    swapId?: number;
}