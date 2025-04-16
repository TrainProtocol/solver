import { BaseRequest } from "./BaseRequest";

export interface EventRequest extends BaseRequest {
    FromBlock: number;
    ToBlock: number;
}