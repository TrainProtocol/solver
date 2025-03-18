import { TransferBuilderRequestBase } from "./TransferBuilderRequestBase";

export interface HTLCRedeemTransferBuilderRequest extends TransferBuilderRequestBase {
    Id: string;
    Secret: string;
}