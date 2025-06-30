import { Network } from "src/treasury/shared/networks.types";
import { SignRequest, SignResponse } from "../treasury.types";
import { PrivateKeyService } from "src/kv/vault.service";
import { GenerateResponse } from "../dto/base.dto";

export abstract class TreasuryService {
    protected constructor(protected readonly privateKeyService: PrivateKeyService) {}
    abstract readonly network: Network;
    abstract sign(req: SignRequest): Promise<SignResponse>;
    abstract generate(): Promise<GenerateResponse>;
}
  