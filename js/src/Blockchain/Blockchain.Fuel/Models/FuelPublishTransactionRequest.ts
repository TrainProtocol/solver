import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface FuelPublishTransactionRequest extends BaseRequest {
  signedRawData: string;
}