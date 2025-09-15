import { BaseRequest } from "./BaseRequest";

export interface AllowanceRequest extends BaseRequest {
    ownerAddress: string;
    asset: string;
  }