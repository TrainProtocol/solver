import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface FuelPublishTransactionRequest extends BaseRequest {
    FromAddress: string;
    CallData: string;
    Fee: Fee;
  }