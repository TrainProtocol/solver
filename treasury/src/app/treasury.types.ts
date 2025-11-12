import { EVMSignRequest, EVMSignResponse } from "../treasury/evm/evm.dto";
import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest } from "../treasury/starknet/starknet.dto";
import { FuelSignRequest } from "../treasury/fuel/fuel.dto";

export type SignRequest =
  (EVMSignRequest
  | StarknetSignRequest
  | FuelSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse) & BaseSignResponse;
