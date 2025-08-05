import { BaseRequest } from "./BaseRequest";

export interface AllowanceRequest extends BaseRequest {
    ownerAddress: string;
    spenderAddress: string;
    asset: string;
  }