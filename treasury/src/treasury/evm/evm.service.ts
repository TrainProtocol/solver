import { BadRequestException, Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { EVMSignRequest, EVMSignResponse } from './evm.dto';
import { Network } from '../shared/networks.types';
import { PrivateKeyService } from '../../kv/vault.service';
import { Transaction, Wallet, isHexString, isAddress } from 'ethers';
import { GenerateResponse } from '../../app/dto/base.dto';
import { SignResponse } from '../../app/treasury.types';

@Injectable()
export class EvmTreasuryService extends TreasuryService {
  readonly network: Network = 'evm';

  constructor(privateKeyService: PrivateKeyService){
    super(privateKeyService);
  }

  async sign(req: EVMSignRequest): Promise<SignResponse> {    
    const signerAddress = req.address.toLowerCase();

    if (!isAddress(signerAddress)){
      throw new BadRequestException(`Invalid ${this.network} address`);
    }

    const privateKey = await this.privateKeyService.getAsync(signerAddress);
    const unsignedTxn = isHexString(req.unsignedTxn) ?  req.unsignedTxn : `0x${req.unsignedTxn}`;

    try {
      const tx = Transaction.from(unsignedTxn)
      const signedTxn = await new Wallet(privateKey).signTransaction(tx);
      return { signedTxn };
    }
    catch (error) {
      throw new BadRequestException(`Invalid unsigned transaction: ${error.message}`);
    }
  }

  async generate(): Promise<GenerateResponse> {
    const wallet  = Wallet.createRandom();
    const address = wallet.address.toLowerCase();
    await this.privateKeyService.setAsync(address, wallet.privateKey);

    return { address };
  }
}