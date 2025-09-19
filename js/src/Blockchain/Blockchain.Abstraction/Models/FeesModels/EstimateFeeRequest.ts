import { BaseRequest } from "../BaseRequest";

export interface EstimateFeeRequest extends BaseRequest {
    toAddress: string,
    amount: number,
    fromAddress: string,
    asset: string,
    callData?: string
    nonce: string
}