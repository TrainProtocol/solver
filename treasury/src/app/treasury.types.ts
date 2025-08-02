import { EVMSignRequest, EVMSignResponse } from "src/treasury/evm/evm.dto";
import { BaseSignRequest, BaseSignResponse } from "./dto/base.dto";
import { StarknetSignRequest, StarknetSignResponse } from "../treasury/starknet/starknet.dto";
import { FuelSignRequest } from "src/treasury/fuel/fuel.dto";

export type SignRequest =
  (EVMSignRequest
  | StarknetSignRequest
  | FuelSignRequest) & BaseSignRequest;

export type SignResponse =
  (EVMSignResponse
  | StarknetSignResponse) & BaseSignResponse;
