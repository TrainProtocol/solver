import { TransferBuilderRequestBase } from "./TransferBuilderRequestBase";

export interface HTLCAddLockSigBuilderRequest extends TransferBuilderRequestBase {
    Id: string;
    Hashlock: string;
    Timelock: string;
    SignatureArray: string[];
    ChainId: string;
    NodeUrl: string;
    SignerAddress: string;
}