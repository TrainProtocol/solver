import { TransferBuilderRequestBase } from "./TransferBuilderRequestBase";

export interface ApproveTransactionBuilderRequest extends TransferBuilderRequestBase{
    Spender: string;
    AmountInWei: string;
}
