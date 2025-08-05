import { EVMSignRequest, EVMSignResponse } from "src/treasury/evm/evm.dto";
import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest, StarknetSignResponse } from "../treasury/starknet/starknet.dto";
import { AztecSignRequest, AztecSignResponse } from "src/treasury/aztec/aztec.dto";

export type SignRequest =
  (EVMSignRequest
  | StarknetSignRequest | AztecSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse
  | StarknetSignResponse| AztecSignResponse) & BaseSignResponse;
