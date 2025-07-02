import { EVMSignRequest, EVMSignResponse } from "src/treasury/evm/evm.dto";
import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest, StarknetSignResponse } from "../treasury/starknet/starknet.dto";

export type SignRequest =
  (EVMSignRequest
  | StarknetSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse
  | StarknetSignResponse) & BaseSignResponse;
