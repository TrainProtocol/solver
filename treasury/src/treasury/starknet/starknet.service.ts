import { Injectable } from '@nestjs/common';
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { Network } from '../shared/networks.types';
import { StarknetSignRequest } from './starknet.dto';
import { BaseSignResponse, GenerateResponse } from '../../app/dto/base.dto';
import { CairoCustomEnum, CairoOption, CairoOptionVariant, Call, CallData, DeployAccountSignerDetails, ec, hash, Invocation, InvocationsSignerDetails, Signer, stark, transaction, uint256  } from 'starknet';
import { PrivateKeyService } from '../../kv/vault.service';

@Injectable()
export class StarknetTreasuryService extends TreasuryService {

  readonly network: Network = 'starknet';
  readonly argentXaccountClassHash = '0x036078334509b514626504edc9fb252328d1a240e4e948bef8d0c08dff45927f';

  constructor(privateKeyService: PrivateKeyService) {
    super(privateKeyService);
  }

  async sign(request: StarknetSignRequest): Promise<BaseSignResponse> {

    const privateKey = await this.privateKeyService.getAsync(request.address);
    const signer = new Signer(privateKey);

    // if (request.type === "Deploy") {
    //   const pubKey = await signer.getPubKey();

    //   const axSigner = new CairoCustomEnum({ Starknet: { pubkey: pubKey } });
    //   const axGuardian = new CairoOption<unknown>(CairoOptionVariant.None);

    //   const AXConstructorCallData = CallData.compile({
    //     owner: axSigner,
    //     guardian: axGuardian,
    //   });

    //   const AXcontractAddress = hash.calculateContractAddressFromHash(
    //     pubKey,
    //     this.argentXaccountClassHash,
    //     AXConstructorCallData,
    //     0
    //   );

    //   const deployAccountPayload = {
    //     classHash: this.argentXaccountClassHash,
    //     constructorCalldata: AXConstructorCallData,
    //     contractAddress: request.address,
    //     addressSalt: pubKey,
    //   };

    //   const deployDetails: DeployAccountSignerDetails =
    //   {

    //   }
    // }
    //else {
      const transferCalls: Call = JSON.parse(request.unsignedTxn);
      const signerDetails: InvocationsSignerDetails = JSON.parse(request.signerInvocationDetails);

      const calldata = transaction.getExecuteCalldata([transferCalls], signerDetails.cairoVersion);
      const signature = await signer.signTransaction([transferCalls], signerDetails);

      const response: Invocation = {
        ...stark.v3Details(signerDetails),
        contractAddress: request.address,
        calldata,
        signature,
      };

      return { signedTxn: this.serializeWithBigInt(response) };
  }

  async generate(): Promise<GenerateResponse> {

    const privateKeyAX = stark.randomAddress();
    const starkKeyPubAX = ec.starkCurve.getStarkKey(privateKeyAX);

    // Calculate future address of the ArgentX account
    const axSigner = new CairoCustomEnum({ Starknet: { pubkey: starkKeyPubAX } });
    const axGuardian = new CairoOption<unknown>(CairoOptionVariant.None);
    const AXConstructorCallData = CallData.compile({
      owner: axSigner,
      guardian: axGuardian,
    });

    const address = hash.calculateContractAddressFromHash(
      starkKeyPubAX,
      this.argentXaccountClassHash,
      AXConstructorCallData,
      0
    );

    return { address }
  }

  private serializeWithBigInt(obj: unknown): string {
    return JSON.stringify(obj, (_key, value) =>
      typeof value === 'bigint' ? uint256.bnToUint256(value) : value
    );
  }
}