import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface FuelPublishTransactionRequest extends BaseRequest {
    fromAddress: string;
    callData: string;
    fee: Fee;
    amount: number;
  }