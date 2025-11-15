import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface PublishTransactionRequest extends BaseRequest {
  signedRawData: string;
  signerInvocationDetails: string;
  nonce: string;
  fromAddress: string;
}