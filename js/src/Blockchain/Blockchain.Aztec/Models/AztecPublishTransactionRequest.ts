import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface AztecPublishTransactionRequest extends BaseRequest {
  signedTx: string;
}