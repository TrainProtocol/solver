import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { AztecSignRequest, AztecSignResponse } from "./aztec.dto";
import { AztecAddress, Contract, ContractFunctionInteraction, createAztecNodeClient, Fr, SponsoredFeePaymentMethod, Tx, waitForPXE } from "@aztec/aztec.js";
import { createPXEService } from "@aztec/pxe/server";
import { createStore } from "@aztec/kv-store/lmdb";
import { deriveSigningKey } from '@aztec/stdlib/keys';
import { TokenContract } from '@aztec/noir-contracts.js/Token';
import { getSponsoredFPCInstance } from "./FPC";
import { getSchnorrAccount, getSchnorrWallet } from "@aztec/accounts/schnorr";
import { getPXEServiceConfig } from "@aztec/pxe/config";
import { TrainContract } from "./Train";
import { SponsoredFPCContract } from "@aztec/noir-contracts.js/SponsoredFPC";
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { PrivateKeyService } from '../../kv/vault.service';
import { BaseSignRequest, BaseSignResponse, GenerateResponse } from '../../app/dto/base.dto';

@Injectable()
export class AztecTreasuryService extends TreasuryService {

    readonly network: Network = 'aztec';

    constructor(privateKeyService: PrivateKeyService) {
        super(privateKeyService);
    }

    async sign(request: BaseSignRequest): Promise<BaseSignResponse> {
    //     const signerAddress = request.address;

    //     try {

    //         const privateKey = Fr.fromString(await this.privateKeyService.getAsync(signerAddress));
    //         const privateSalt = Fr.fromString(await this.privateKeyService.getAsync(signerAddress + "1"));//just assuming that salt should save by adding 1 to the end 

    //         const TrainContractArtifact = TrainContract.artifact;
    //         const TokenContractArtifact = TokenContract.artifact;

    //         const provider = createAztecNodeClient(request.nodeUrl);

    //         const fullConfig = {
    //             ...getPXEServiceConfig(),
    //             l1Contracts: await provider.getL1ContractAddresses(),
    //         };

    //         const store = await createStore('store', {
    //             dataDirectory: 'store',
    //             dataStoreMapSizeKB: 1e6,
    //         });

    //         const options: PXECreationOptions = {
    //             loggers: {},
    //             store,
    //         };

    //         const pxe = await createPXEService(provider, fullConfig, options);
    //         await waitForPXE(pxe);

    //         const schnorrAccount = await getSchnorrAccount(
    //             pxe,
    //             privateKey,
    //             deriveSigningKey(privateKey),
    //             privateSalt
    //         );
    //         await schnorrAccount.register();

    //         const schnorrWallet = await schnorrAccount.getWallet();
    //         const tokenContractInstance = await provider.getContract(AztecAddress.fromString(request.tokenContract))
    //         await pxe.registerContract({
    //             instance: tokenContractInstance,
    //             artifact: TokenContractArtifact,
    //         });

    //         const tokenInstance = await Contract.at(
    //             AztecAddress.fromString(request.tokenContract),
    //             TokenContractArtifact,
    //             schnorrWallet,
    //         );

    //         const htlcContractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(request.htlcContractAddress))
    //         await pxe.registerContract({
    //             instance: htlcContractInstanceWithAddress,
    //             artifact: TrainContractArtifact,
    //         })
    //         const htlcContract = await Contract.at(
    //             AztecAddress.fromString(request.htlcContractAddress),
    //             TokenContractArtifact,
    //             schnorrWallet,
    //         );

    //         const sponsoredFPC = await getSponsoredFPCInstance();
    //         const paymentMethod = new SponsoredFeePaymentMethod(sponsoredFPC.address);
    //         await pxe.registerContract({
    //             instance: sponsoredFPC,
    //             artifact: SponsoredFPCContract.artifact,
    //         });
    //         //////////////////////////////////////////
    //         const contractFunctionInteraction = request.functionInteractions[0];
    //         const transferFunctionInteraction = request.functionInteractions[1]!;

    //         const transferFuncInteraction: ContractFunctionInteraction =
    //             new ContractFunctionInteraction(
    //                 schnorrWallet,
    //                 contractFunctionInteraction.contractAddress,
    //                 contractFunctionInteraction.functionInteractionAbi,
    //                 [
    //                     ...contractFunctionInteraction.args
    //                 ],
    //             );

    //         let functionInteraction: ContractFunctionInteraction;

    //         const witness = await schnorrWallet.createAuthWit({
    //             caller: AztecAddress.fromString(request.htlcContractAddress),
    //             action: transferFuncInteraction,
    //         });

    //         if (transferFunctionInteraction) {
    //             functionInteraction = new ContractFunctionInteraction(
    //                 schnorrWallet,
    //                 transferFunctionInteraction.contractAddress,
    //                 transferFunctionInteraction.functionInteractionAbi,
    //                 [
    //                     ...transferFunctionInteraction.args
    //                 ],
    //                 [witness],
    //             );
    //         }
    //         else {
    //             functionInteraction = new ContractFunctionInteraction(
    //                 schnorrWallet,
    //                 transferFunctionInteraction.contractAddress,
    //                 transferFunctionInteraction.functionInteractionAbi,
    //                 [
    //                     ...transferFunctionInteraction.args
    //                 ]
    //             );

    //             const provenTx = await functionInteraction.prove({
    //                 fee: { paymentMethod },
    //             });
    //         }
    //         // const tx = new Tx(
    //         //     provenTx.data,
    //         //     provenTx.clientIvcProof,
    //         //     provenTx.contractClassLogFields,
    //         //     provenTx.publicFunctionCalldata,
    //         // );

            return null;

        // }
        // catch (error) {
        //     throw new BadRequestException(`Invalid unsigned transaction: ${error.message}`);
        // }
    }

    generate(): Promise<GenerateResponse> {
        throw new Error("Method not implemented.");
    }
}