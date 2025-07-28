import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface StarknetPublishTransactionRequest extends BaseRequest {
    fromAddress: string;
    callData: string;
    nonce: string;
    fee: Fee;
  }