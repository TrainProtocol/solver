import { Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { Network } from '../shared/networks.types';
import { StarknetSignRequest } from './starknet.dto';
import { PrivateKeyService } from 'src/kv/vault.service';
import { BaseSignResponse, GenerateResponse } from '../../app/dto/base.dto';
import { Call, ec, Invocation, InvocationsSignerDetails, Signer, stark, transaction } from 'starknet';

@Injectable()
export class StarknetTreasuryService extends TreasuryService {

  readonly network: Network = 'starknet';

  constructor(privateKeyService: PrivateKeyService) {
    super(privateKeyService);
  }

  async sign(request: StarknetSignRequest): Promise<BaseSignResponse> {
    const privateKey = await this.privateKeyService.getAsync(request.address);

    const transferCalls: Call = JSON.parse(request.unsignedTxn);
    const signerDetails: InvocationsSignerDetails = JSON.parse(request.signerInvocationDetails);

    const calldata = transaction.getExecuteCalldata([transferCalls], signerDetails.cairoVersion);
    const signature = await new Signer(privateKey).signTransaction([transferCalls], signerDetails);

    const response: Invocation = {
      ...stark.v3Details(signerDetails),
      contractAddress: request.address,
      calldata,
      signature,
    };

    return { signedTxn: JSON.stringify(response) };
  }

  async generate(): Promise<GenerateResponse> {
    throw new Error('Method not implemented.');
  }
}