import { BaseRequest } from "../BaseRequest";

export interface BalanceRequest extends BaseRequest {
    address: string;
    asset: string;
  }
  