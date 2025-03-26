import { BalanceRequest } from "./BalanceRequest";

export interface SufficientBalanceRequest extends BalanceRequest {
    Amount: number;
  }