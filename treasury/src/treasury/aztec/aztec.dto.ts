import { BaseSignRequest } from "../../app/dto/base.dto";

export class AztecSignRequest extends BaseSignRequest {
    nodeUrl: string;
    tokenContract: string;
    contractAddress: string
}