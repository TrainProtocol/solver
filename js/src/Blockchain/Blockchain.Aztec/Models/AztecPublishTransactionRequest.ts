import { Tx } from "@aztec/aztec.js";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";

export interface AztecPublishTransactionRequest extends BaseRequest {
    fromAddress: string;
    callData: string;
    fee: Fee;
    amount: number;
    tx: Tx
  }