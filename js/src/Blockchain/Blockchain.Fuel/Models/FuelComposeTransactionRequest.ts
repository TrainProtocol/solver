import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface FuelComposeTransactionRequest extends BaseRequest {
    fromAddress: string;
    callData: string;
    callDataAsset : string;
    callDataAmount: number;
}
