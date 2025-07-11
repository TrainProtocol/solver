import { Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { Network } from '../shared/networks.types';
import { StarknetSignRequest, StarknetSignResponse } from './starknet.dto';
import { PrivateKeyService } from 'src/kv/vault.service';
import { GenerateResponse } from '../../app/dto/base.dto';

@Injectable()
export class StarknetTreasuryService extends TreasuryService {
 
  readonly network: Network = 'starknet';

  constructor(privateKeyService: PrivateKeyService){
    super(privateKeyService);
  }

  // TODO: Implement the Starknet signing logic
  sign(req: StarknetSignRequest): Promise<StarknetSignResponse> {
    throw new Error('Method not implemented.');
  }

  // TODO: Implement the Starknet address generation(if applicable)
  generate(): Promise<GenerateResponse> {
    throw new Error('Method not implemented.');
  }
}