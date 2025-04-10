import { BaseRequest } from "../BaseRequest";

export interface EstimateFeeRequest extends BaseRequest {
    ToAddress: string,
    Amount: number,
    FromAddress: string,
    Asset: string,
    CallData?: string
}