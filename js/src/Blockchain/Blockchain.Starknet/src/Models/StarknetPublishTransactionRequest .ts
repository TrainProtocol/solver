import { BaseRequest, Fee } from "@blockchain/common";

export interface StarknetPublishTransactionRequest extends BaseRequest {
  FromAddress: string;
  CallData: string;
  Nonce: string;
  Fee: Fee;
}