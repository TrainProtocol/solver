import { BaseRequest } from "../BaseRequest";

export interface CurrentNonceRequest extends BaseRequest {
    address: string;
    currentNonce: number;
}