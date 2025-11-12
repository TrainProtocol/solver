import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface PublishTransactionRequest extends BaseRequest {
  signedRawData: string;
}

export interface SimulateTransactionRequest extends PublishTransactionRequest {
  nonce: string;
}