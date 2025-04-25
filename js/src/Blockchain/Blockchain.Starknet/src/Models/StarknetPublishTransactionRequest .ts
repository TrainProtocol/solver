import { BaseRequest } from "src/Common/Abstraction/Models/BaseRequest";
import { Fee } from "src/Common/Abstraction/Models/FeesModels/Fee";


export interface StarknetPublishTransactionRequest extends BaseRequest {
    FromAddress: string;
    CallData: string;
    Nonce: string;
    Fee: Fee;
  }