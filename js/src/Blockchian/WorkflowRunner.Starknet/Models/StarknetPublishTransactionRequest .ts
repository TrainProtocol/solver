import { Fee } from "../../../CoreAbstraction/Models/FeesModels/Fee";

export interface StarknetPublishTransactionRequest {
    networkName: string;
    fromAddress: string;
    callData: string;
    nonce: string;
    fee: Fee;
  }