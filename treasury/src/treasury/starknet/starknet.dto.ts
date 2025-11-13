import { BaseSignRequest, BaseSignResponse } from "../../app/dto/base.dto";

export class StarknetSignRequest extends BaseSignRequest {
    signerInvocationDetails: string;
}