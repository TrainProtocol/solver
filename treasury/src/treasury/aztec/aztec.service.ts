import { TreasuryService } from "src/app/interfaces/treasury.interface";
import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { PrivateKeyService } from "src/kv/vault.service";
import { AztecSignRequest, AztecSignResponse } from "./aztec.dto";
import { GenerateResponse } from "src/app/dto/base.dto";

@Injectable()
export class FuelTreasuryService extends TreasuryService {
    readonly network: Network = 'fuel';

    constructor(privateKeyService: PrivateKeyService) {
        super(privateKeyService);
    }

    async sign(req: AztecSignRequest): Promise<AztecSignResponse> {
        const signerAddress = req.address.toLowerCase();

        if (!new Address(signerAddress)) {
            throw new BadRequestException(`Invalid ${this.network} address`);
        }

        const privateKey = await this.privateKeyService.getAsync(signerAddress);

        try {
            const requestData = JSON.parse(req.unsignedTxn);
            const wallet = Wallet.fromPrivateKey(privateKey)
            const isTxnTypeScript = isTransactionTypeScript(JSON.parse(req.unsignedTxn));

            if (!isTxnTypeScript) {
                throw new Error("Transaction is not of type Script");
            }

            const txRequest = ScriptTransactionRequest.from(transactionRequestify(requestData));

            const signedTxn = await wallet.signTransaction(txRequest);
            
            return { signedTxn };
        }
        catch (error) {
            throw new BadRequestException(`Invalid unsigned transaction: ${error.message}`);
        }
    }

    async generate(): Promise<GenerateResponse> {
        const wallet = Wallet.generate();
        const address = wallet.address.toAddress().toLowerCase();
        await this.privateKeyService.setAsync(address, wallet.privateKey);

        return { address };
    }
}