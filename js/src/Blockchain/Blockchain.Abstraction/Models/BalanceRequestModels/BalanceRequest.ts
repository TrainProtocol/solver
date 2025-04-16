import { BaseRequest } from "../BaseRequest";

export interface BalanceRequest extends BaseRequest {
    Address: string;
    Asset: string;
  }
  