import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { Address, isTransactionTypeScript, Provider, ScriptTransactionRequest, transactionRequestify, Wallet } from 'fuels';
import { FuelSignRequest } from "./fuel.dto";
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { PrivateKeyService } from '../../kv/vault.service';
import { BaseSignResponse, GenerateResponse } from '../../app/dto/base.dto';

@Injectable()
export class FuelTreasuryService extends TreasuryService {
    readonly network: Network = 'fuel';

    constructor(privateKeyService: PrivateKeyService) {
        super(privateKeyService);
    }

    async sign(request: FuelSignRequest): Promise<BaseSignResponse> {

        if (!new Address(request.address)) {
            throw new BadRequestException(`Invalid ${this.network} address`);
        }

        const privateKey = await this.privateKeyService.getAsync(request.address);

        try {
            const provider = new Provider(request.nodeUrl);
            const requestData = JSON.parse(request.unsignedTxn);
            const wallet = Wallet.fromPrivateKey(privateKey, provider);

            const isTxnTypeScript = isTransactionTypeScript(JSON.parse(request.unsignedTxn));

            if (!isTxnTypeScript) {
                throw new BadRequestException("Transaction is not of type Script");
            }

            const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));

            txRequest.witnesses[0] = await wallet.signTransaction(txRequest)

            await wallet.simulateTransaction(txRequest);

            return { signedTxn: JSON.stringify(txRequest)};
        }
        catch (error) {
            throw error;
        }
    }

    async generate(): Promise<GenerateResponse> {
        const wallet = Wallet.generate();
        const address = wallet.address.toAddress().toLowerCase();
        await this.privateKeyService.setAsync(address, wallet.privateKey);

        return { address };
    }
}
