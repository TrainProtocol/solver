import { EVMSignRequest, EVMSignResponse } from "../treasury/evm/evm.dto";
import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest, StarknetSignResponse } from "../treasury/starknet/starknet.dto";
import { FuelSignRequest } from "../treasury/fuel/fuel.dto";
import { AztecSignRequest } from "../treasury/aztec/aztec.dto";

export type SignRequest =
  (EVMSignRequest
    | StarknetSignRequest
    | FuelSignRequest
    | AztecSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse
    | StarknetSignResponse) & BaseSignResponse;
