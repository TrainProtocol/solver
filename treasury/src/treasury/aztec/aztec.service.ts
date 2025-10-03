import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { AztecSignRequest, AztecSignResponse } from "./aztec.dto";
import { AuthWitness, AztecAddress, ContractArtifact, ContractFunctionInteraction, createAztecNodeClient, Fr, FunctionAbi, getAllFunctionAbis, Logger, SponsoredFeePaymentMethod, Tx, waitForPXE } from "@aztec/aztec.js";
import { createPXEService } from "@aztec/pxe/server";
import { createStore } from "@aztec/kv-store/lmdb";
import { deriveSigningKey } from '@aztec/stdlib/keys';
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
import { AztecConfigService } from './aztec.config';

@Injectable()
export class AztecTreasuryService extends TreasuryService {

    readonly network: Network = 'aztec';
    readonly configService: AztecConfigService;

    constructor(privateKeyService: PrivateKeyService, configService: AztecConfigService) {
        super(privateKeyService);
        this.configService = configService;
    }

    async sign(request: AztecSignRequest): Promise<AztecSignResponse> {
        try {

            const privateKey = await this.privateKeyService.getAsync(request.address);
            const privateSalt = await this.privateKeyService.getAsync(request.address, "private_salt");
            const TrainContractArtifact = TrainContract.artifact;
            const TokenContractArtifact = TokenContract.artifact;

            // Define the type locally
            type PXECreationOptions = {
                loggers?: { store?: Logger; pxe?: Logger; prover?: Logger };
                useLogSuffix?: boolean | string;
                prover?: PrivateKernelProver;
                store?: AztecAsyncKVStore;
            };

            const provider = createAztecNodeClient(request.nodeUrl);

            const fullConfig = {
                ...getPXEServiceConfig(),
                l1Contracts: await provider.getL1ContractAddresses(),
            };

            const store = await createStore(request.address, {
                dataDirectory: this.configService.storePath,
                dataStoreMapSizeKB: 1e6,
            });

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
                Fr.fromString(privateKey),
                deriveSigningKey(Fr.fromString(privateKey)),
                Fr.fromString(privateSalt)
            );

            await schnorrAccount.register();

            const schnorrWallet = await schnorrAccount.getWallet();
            const tokenContractInstance = await provider.getContract(AztecAddress.fromString(request.tokenContract));

            await pxe.registerContract({
                instance: tokenContractInstance,
                artifact: TokenContractArtifact,
            });

            const contractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(request.contractAddress))

            await pxe.registerContract({
                instance: contractInstanceWithAddress,
                artifact: TrainContractArtifact,
            })

            const contractFunctionInteraction: FunctionInteraction = JSON.parse(request.unsignedTxn);
            let authWitnesses: AuthWitness[] = [];

            if (contractFunctionInteraction.authwiths) {

                contractFunctionInteraction.authwiths.forEach(async (authWith) => {

                    const requestContractClass = await provider.getContract(AztecAddress.fromString(authWith.interactionAddress))
                    const contractClassMetadata = await pxe.getContractClassMetadata(requestContractClass.currentContractClassId, true)

                    if (!contractClassMetadata.artifact) {
                        throw new BadRequestException(`Artifact not registered`);
                    }

                    const functionAbi = getFunctionAbi(contractClassMetadata.artifact, authWith.functionName);

                    if (!functionAbi) {
                        throw new BadRequestException("Unable to get function ABI");
                    }

                    authWith.args.unshift(schnorrWallet.getAddress());

                    const functionInteraction = new ContractFunctionInteraction(
                        schnorrWallet,
                        AztecAddress.fromString(authWith.interactionAddress),
                        functionAbi,
                        [
                            ...authWith.args
                        ],
                    );

                    const witness = await schnorrWallet.createAuthWit({
                        caller: AztecAddress.fromString(authWith.callerAddress),
                        action: functionInteraction,
                    });

                    authWitnesses.push(witness);
                });
            }

            const requestcontractClass = await provider.getContract(AztecAddress.fromString(contractFunctionInteraction.interactionAddress))
            const contractClassMetadata = await pxe.getContractClassMetadata(requestcontractClass.currentContractClassId, true)

            if (!contractClassMetadata.artifact) {
                throw new BadRequestException(`Artifact not registered`);
            }

            const functionAbi = getFunctionAbi(contractClassMetadata.artifact, contractFunctionInteraction.functionName);

            const functionInteraction = new ContractFunctionInteraction(
                schnorrWallet,
                AztecAddress.fromString(contractFunctionInteraction.interactionAddress),
                functionAbi,
                [
                    ...contractFunctionInteraction.args
                ],
                [...authWitnesses]
            );

            const provenTx = await functionInteraction.prove({ from: AztecAddress.fromString(request.address), fee: { paymentMethod } });

            const tx = new Tx(
                provenTx.getTxHash(),
                provenTx.data,
                provenTx.clientIvcProof,
                provenTx.contractClassLogFields,
                provenTx.publicFunctionCalldata,
            );

            const signedTxHex = tx.toBuffer().toString("hex");
            const signedTxn = JSON.stringify({ signedTx: signedTxHex });

            return { signedTxn };

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

        await createStore(address, {
            dataDirectory: this.configService.storePath,
            dataStoreMapSizeKB: 1e6,
        });

        return { address };
    }
}

interface FunctionInteraction {
    interactionAddress: string,
    functionName: string,
    args: any[],
    callerAddress?: string,
    senderAddress?: string,
    authwiths?: FunctionInteraction[],
}

function getFunctionAbi(
    artifact: ContractArtifact,
    fnName: string,
): FunctionAbi | undefined {
    const fn = getAllFunctionAbis(artifact).find(({ name }) => name === fnName);
    if (!fn) { }
    return fn;
}