import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface EnsureSufficientBalanceRequest extends BaseRequest{
    address: string,
    asset: string,
    amount: string,
    feeAmount: string
}