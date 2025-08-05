import { Tx } from "@aztec/aztec.js";
import { BaseSignRequest } from "src/app/dto/base.dto";

export class AztecSignRequest extends BaseSignRequest {
    nodeUrl: string;
    htlcContractAddress: string;
    tokenContract: string;
    functionInteractions: any[];    
}

export class AztecSignResponse {
    tx: Tx
}