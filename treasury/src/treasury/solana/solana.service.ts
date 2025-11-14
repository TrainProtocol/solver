import { Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { Network } from '../shared/networks.types';
import { BaseSignRequest, BaseSignResponse, GenerateResponse } from '../../app/dto/base.dto';
import { PrivateKeyService } from '../../kv/vault.service';
import { Keypair, Transaction } from "@solana/web3.js";
import bs58 from "bs58";

@Injectable()
export class SolanaTreasuryService extends TreasuryService {

  readonly network: Network = 'solana';

  constructor(privateKeyService: PrivateKeyService) {
    super(privateKeyService);
  }

  async sign(request: BaseSignRequest): Promise<BaseSignResponse> {
    
    const privateKey = await this.privateKeyService.getAsync(request.address);

    const account = Keypair.fromSecretKey(bs58.decode(privateKey));

    let transaction: Transaction = Transaction.from(Buffer.from(request.unsignedTxn, 'base64'));

    transaction.sign(account);

    return { signedTxn: transaction.serialize().toString('base64') };
  }

  async generate(): Promise<GenerateResponse> {
    const keypair = Keypair.generate();

    await this.privateKeyService.setAsync(keypair.publicKey.toBase58(), keypair.secretKey.toString());

    return { address: keypair.publicKey.toBase58() };
  }
}