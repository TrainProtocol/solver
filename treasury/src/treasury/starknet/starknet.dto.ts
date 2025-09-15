import { BaseSignRequest, BaseSignResponse } from "src/app/dto/base.dto";

export class StarknetSignRequest extends BaseSignRequest {
    signerInvocationDetails: string;
}