import { BaseRequest } from "./BaseRequest";

export interface AllowanceRequest extends BaseRequest {
    OwnerAddress: string;
    SpenderAddress: string;
    Asset: string;
  }