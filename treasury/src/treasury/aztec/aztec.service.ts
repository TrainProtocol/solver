import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { AztecSignRequest, AztecSignResponse } from "./aztec.dto";
import { AztecAddress, ContractFunctionInteraction, createAztecNodeClient, Fr, getAccountContractAddress, Grumpkin, Logger, SponsoredFeePaymentMethod, Tx, waitForPXE } from "@aztec/aztec.js";
import { createPXEService } from "@aztec/pxe/server";
import { createStore } from "@aztec/kv-store/lmdb";
import { deriveKeys, derivePublicKeyFromSecretKey, deriveSigningKey } from '@aztec/stdlib/keys';
import { TokenContract } from '@aztec/noir-contracts.js/Token';
import { getSponsoredFPCInstance } from "./FPC";
import { getSchnorrAccount, getSchnorrAccountContractAddress } from "@aztec/accounts/schnorr";
import { getPXEServiceConfig } from "@aztec/pxe/config";
import { TrainContract } from "./Train";
import { SponsoredFPCContract } from "@aztec/noir-contracts.js/SponsoredFPC";
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { PrivateKeyService } from '../../kv/vault.service';
import { GenerateResponse } from '../../app/dto/base.dto';
import { PrivateKernelProver } from '@aztec/stdlib/interfaces/client';
import { AztecAsyncKVStore } from '@aztec/kv-store';

@Injectable()
export class AztecTreasuryService extends TreasuryService {

    readonly network: Network = 'aztec';

    constructor(privateKeyService: PrivateKeyService) {
        super(privateKeyService);
    }

    async sign(request: AztecSignRequest): Promise<AztecSignResponse> {
        const signerAddress = request.address;

        try {

            const privateKey = Fr.fromString(await this.privateKeyService.getAsync(signerAddress));
            const privateSalt = Fr.fromString(await this.privateKeyService.getAsync(signerAddress, "private_salt"));

            const TrainContractArtifact = TrainContract.artifact;
            const TokenContractArtifact = TokenContract.artifact;

            const provider = createAztecNodeClient(request.nodeUrl);

            const fullConfig = {
                ...getPXEServiceConfig(),
                l1Contracts: await provider.getL1ContractAddresses(),
            };

            const store = await createStore('store', {
                dataDirectory: 'store',
                dataStoreMapSizeKB: 1e6,
            });

            // Define the type locally
            type PXECreationOptions = {
                loggers?: { store?: Logger; pxe?: Logger; prover?: Logger };
                useLogSuffix?: boolean | string;
                prover?: PrivateKernelProver;
                store?: AztecAsyncKVStore;
            };

            const options: PXECreationOptions = {
                loggers: {},
                store,
            };

            const pxe = await createPXEService(provider, fullConfig, options);
            await waitForPXE(pxe);

            const sponsoredFPC = await getSponsoredFPCInstance();
            const paymentMethod = new SponsoredFeePaymentMethod(sponsoredFPC.address);
            await pxe.registerContract({
                instance: sponsoredFPC,
                artifact: SponsoredFPCContract.artifact,
            });

            const schnorrAccount = await getSchnorrAccount(
                pxe,
                privateKey,
                deriveSigningKey(privateKey),
                privateSalt
            );
            await schnorrAccount.register();

            const schnorrWallet = await schnorrAccount.getWallet();
            const tokenContractInstance = await provider.getContract(AztecAddress.fromString(request.tokenContract))
            await pxe.registerContract({
                instance: tokenContractInstance,
                artifact: TokenContractArtifact,
            });

            // const tokenInstance = await Contract.at(
            //     AztecAddress.fromString(request.tokenContract),
            //     TokenContractArtifact,
            //     schnorrWallet,
            // );

            const htlcContractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(request.htlcContractAddress))
            await pxe.registerContract({
                instance: htlcContractInstanceWithAddress,
                artifact: TrainContractArtifact,
            })

            // const htlcContract = await Contract.at(
            //     AztecAddress.fromString(request.htlcContractAddress),
            //     TokenContractArtifact,
            //     schnorrWallet,
            // );


            //////////////////////////////////////////
            const contractFunctionInteraction = request.functionInteractions[0];
            const transferFunctionInteraction = request.functionInteractions[1]!;
            let functionInteraction: ContractFunctionInteraction;

            if (transferFunctionInteraction) {

                const witness = await schnorrWallet.createAuthWit({
                    caller: AztecAddress.fromString(request.htlcContractAddress),
                    action: transferFunctionInteraction,
                });

                transferFunctionInteraction.args.unshift(schnorrWallet.getAddress())

                functionInteraction = new ContractFunctionInteraction(
                    schnorrWallet,
                    transferFunctionInteraction.contractAddress,
                    transferFunctionInteraction.functionInteractionAbi,
                    [
                        ...transferFunctionInteraction.args
                    ],
                    [witness],
                );
            }
            else {
                //should add  sender address in contractfunctioninteraction
                functionInteraction = new ContractFunctionInteraction(
                    schnorrWallet,
                    contractFunctionInteraction.contractAddress,
                    contractFunctionInteraction.functionInteractionAbi,
                    [
                        ...contractFunctionInteraction.args
                    ],
                );

                const provenTx = await functionInteraction.prove({
                    fee: { paymentMethod },
                });
            }

            // const tx = new Tx(
            //     provenTx.data,
            //     provenTx.clientIvcProof,
            //     provenTx.contractClassLogFields,
            //     provenTx.publicFunctionCalldata,
            // );

            return null;

        }
        catch (error) {
            throw new BadRequestException(`Invalid unsigned transaction: ${error.message}`);
        }
    }

    async generate(): Promise<GenerateResponse> {

        const prKey = Fr.random();
        const salt = Fr.random();
        const addressResponse = await getSchnorrAccountContractAddress(prKey, salt);

        await this.privateKeyService.setAsync(addressResponse.toString(), prKey.toString());
        await this.privateKeyService.setAsync(addressResponse.toString(), salt.toString(), "private_salt");

        const address = addressResponse.toString()

        return { address };

    }
}