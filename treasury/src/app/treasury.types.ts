import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest, StarknetSignResponse } from "../treasury/starknet/starknet.dto";
import { EVMSignRequest, EVMSignResponse } from "../treasury/evm/evm.dto";
import { AztecSignRequest, AztecSignResponse } from "../treasury/aztec/aztec.dto";

export type SignRequest =
  (EVMSignRequest
  | StarknetSignRequest | AztecSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse
  | StarknetSignResponse| AztecSignResponse) & BaseSignResponse;
