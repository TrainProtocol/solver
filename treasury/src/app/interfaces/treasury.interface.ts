import { SignRequest, SignResponse } from "../treasury.types";
import { GenerateResponse } from "../dto/base.dto";
import { Network } from "../../treasury/shared/networks.types";
import { PrivateKeyService } from "../../kv/vault.service";

export abstract class TreasuryService {
    protected constructor(protected readonly privateKeyService: PrivateKeyService) {}
    abstract readonly network: Network;
    abstract sign(req: SignRequest): Promise<SignResponse>;
    abstract generate(): Promise<GenerateResponse>;
}
