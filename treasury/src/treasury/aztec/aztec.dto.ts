import { BaseSignRequest, BaseSignResponse } from "../../app/dto/base.dto";

export class AztecSignRequest extends BaseSignRequest {
    nodeUrl: string;
    tokenContract: string;
    contractAddress: string
}

export class AztecSignResponse extends BaseSignResponse{
}