import { TransferBuilderRequestBase } from "./TransferBuilderRequestBase";

export interface TransferBuilderRequest extends TransferBuilderRequestBase{
    ToAddress: string;
    AmountInWei: string;
}