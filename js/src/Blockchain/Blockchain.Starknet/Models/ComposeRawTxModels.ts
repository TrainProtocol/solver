import { ResourceBounds } from "starknet";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface ComposeRawTransactionRequest extends BaseRequest {
    callData: string,
    nonce: string,
    address: string
    resourceBounds: ResourceBounds
}

export interface ComposeRawTransactionResponse {
    unsignedTxn: string,
    signerInvocationDetails: string
}