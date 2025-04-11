import { BaseRequest } from "../../../CoreAbstraction/Models/BaseRequest";
import { Fee } from "../../../CoreAbstraction/Models/FeesModels/Fee";

export interface StarknetPublishTransactionRequest extends BaseRequest {
    FromAddress: string;
    CallData: string;
    Nonce: string;
    Fee: Fee;
  }