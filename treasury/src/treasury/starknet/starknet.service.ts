import { Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { Network } from '../shared/networks.types';
import { StarknetSignRequest, StarknetSignResponse } from './starknet.dto';
import { GenerateResponse } from '../../app/dto/base.dto';
import { PrivateKeyService } from '../../kv/vault.service';
import { SignResponse } from '../../app/treasury.types';

@Injectable()
export class StarknetTreasuryService extends TreasuryService {
 
  readonly network: Network = 'starknet';

  constructor(privateKeyService: PrivateKeyService){
    super(privateKeyService);
  }

  // TODO: Implement the Starknet signing logic
  sign(req: StarknetSignRequest): Promise<SignResponse> {
    throw new Error('Method not implemented.');
  }

  // TODO: Implement the Starknet address generation(if applicable)
  generate(): Promise<GenerateResponse> {
    throw new Error('Method not implemented.');
  }
}