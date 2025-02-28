import { TransferBuilderRequestBase } from "./HTLCLockTransferBuilderRequest";

export interface HTLCRedeemTransferBuilderRequest extends TransferBuilderRequestBase {
    Id: string;
    Secret: string;
}