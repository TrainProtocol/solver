import { BaseRequest } from "./BaseRequest";

export interface EventRequest extends BaseRequest {
    fromBlock: number;
    toBlock: number;
    walletAddresses: string[];
}