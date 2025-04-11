import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface StarknetPublishTransactionRequest extends BaseRequest {
    FromAddress: string;
    CallData: string;
    Nonce: string;
    Fee: Fee;
  }