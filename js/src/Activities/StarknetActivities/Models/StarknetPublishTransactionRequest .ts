import { Fee } from "../../../lib/Model/GetFeesModels/GetFeesResponse";

export interface StarknetPublishTransactionRequest {
    networkName: string;
    fromAddress: string;
    callData: string;
    nonce: string;
    fee: Fee;
  }