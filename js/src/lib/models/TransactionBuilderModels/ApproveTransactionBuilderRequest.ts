import { TransferBuilderRequestBase } from "./HTLCLockTransferBuilderRequest";

export interface ApproveTransactionBuilderRequest extends TransferBuilderRequestBase{
    Spender: string;
    AmountInWei: string;
}
