import { BaseRequest } from "../BaseRequest";

export interface NextNonceRequest extends BaseRequest {
    address: string;
}