import { BadRequestException, Injectable } from '@nestjs/common';
import { Network } from "../shared/networks.types";
import { AztecSignRequest, AztecSignResponse } from "./aztec.dto";
import { TrainContract } from "./Train";
import { TreasuryService } from '../../app/interfaces/treasury.interface';
import { PrivateKeyService } from '../../kv/vault.service';
import { GenerateResponse } from '../../app/dto/base.dto';
import { AztecConfigService } from './aztec.config';
import { Tx } from "@aztec/aztec.js/tx";
import { ContractFunctionInteraction, getContractInstanceFromInstantiationParams, toSendOptions } from '@aztec/aztec.js/contracts';
import { SponsoredFeePaymentMethod } from '@aztec/aztec.js/fee';
import { SponsoredFPCContract } from '@aztec/noir-contracts.js/SponsoredFPC';
import { TestWallet } from '@aztec/test-wallet/server';
import { createStore } from '@aztec/kv-store/lmdb';
import { AztecNode, createAztecNodeClient } from '@aztec/aztec.js/node';
import { getPXEConfig } from '@aztec/pxe/server';
import { Fr } from '@aztec/aztec.js/fields';
import { deriveSigningKey } from '@aztec/stdlib/keys';
import { AztecAddress } from '@aztec/aztec.js/addresses';
import { TokenContract } from '@aztec/noir-contracts.js/Token';
import { AuthWitness } from '@aztec/stdlib/auth-witness';
import { ContractFunctionInteractionCallIntent } from '@aztec/aztec.js/authorization';
import { ContractArtifact, FunctionAbi, getAllFunctionAbis } from '@aztec/aztec.js/abi';
import { SchnorrAccountContract } from '@aztec/accounts/schnorr';
import { getAccountContractAddress } from '@aztec/aztec.js/account';

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
            const provider: AztecNode = createAztecNodeClient(request.nodeUrl);
            const l1Contracts = await provider.getL1ContractAddresses();

            const fullConfig = { ...getPXEConfig(), l1Contracts, proverEnabled: true };

            const accountContract = new SchnorrAccountContract(deriveSigningKey(Fr.fromString(privateKey)));
            const solverAddress = (await getAccountContractAddress(accountContract, Fr.fromString(privateKey), Fr.fromString(privateSalt))).toString();

            const store = await createStore(request.address, {
                dataDirectory: this.configService.storePath,
                dataStoreMapSizeKb: 1e6,
            });

            const pxe = await TestWallet.create(provider, fullConfig, { store });

            const sponsoredFPCInstance = await getContractInstanceFromInstantiationParams(
                SponsoredFPCContract.artifact,
                { salt: new Fr(0) },
            );

            await pxe.registerContract(
                sponsoredFPCInstance,
                SponsoredFPCContract.artifact,
            );

            await pxe.createSchnorrAccount(
                Fr.fromString(privateKey),
                Fr.fromString(privateSalt),
                deriveSigningKey(Fr.fromString(privateKey)),
            );

            const contractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(request.contractAddress));
            await pxe.registerContract(contractInstanceWithAddress, TrainContract.artifact);

            const tokenInstance = await provider.getContract(AztecAddress.fromString(request.tokenContract));
            await pxe.registerContract(tokenInstance, TokenContract.artifact)

            const contractFunctionInteraction: FunctionInteraction = JSON.parse(request.unsignedTxn);
            let authWitnesses: AuthWitness[] = [];

            if (contractFunctionInteraction.authwiths) {
                for (const authWith of contractFunctionInteraction.authwiths) {
                    const requestContractClass = await provider.getContract(AztecAddress.fromString(authWith.interactionAddress));
                    const contractClassMetadata = await pxe.getContractClassMetadata(requestContractClass.currentContractClassId, true);

                    if (!contractClassMetadata.artifact) {
                        throw new BadRequestException(`Artifact not registered`);
                    }

                    const functionAbi = getFunctionAbi(contractClassMetadata.artifact, authWith.functionName);

                    if (!functionAbi) {
                        throw new BadRequestException("Unable to get function ABI");
                    }

                    authWith.args.unshift(solverAddress);

                    const functionInteraction = new ContractFunctionInteraction(
                        pxe,
                        AztecAddress.fromString(authWith.interactionAddress),
                        functionAbi,
                        [...authWith.args],
                    );

                    const intent: ContractFunctionInteractionCallIntent = {
                        caller: AztecAddress.fromString(authWith.callerAddress),
                        action: functionInteraction,
                    };

                    const witness = await pxe.createAuthWit(
                        AztecAddress.fromString(solverAddress),
                        intent,
                    );

                    authWitnesses.push(witness);
                }
            }

            const requestcontractClass = await provider.getContract(AztecAddress.fromString(contractFunctionInteraction.interactionAddress))
            const contractClassMetadata = await pxe.getContractClassMetadata(requestcontractClass.currentContractClassId, true)

            if (!contractClassMetadata.artifact) {
                throw new BadRequestException(`Artifact not registered`);
            }

            const functionAbi = getFunctionAbi(contractClassMetadata.artifact, contractFunctionInteraction.functionName);

            const functionInteraction = new ContractFunctionInteraction(
                pxe,
                AztecAddress.fromString(contractFunctionInteraction.interactionAddress),
                functionAbi,
                [
                    ...contractFunctionInteraction.args
                ],
                [...authWitnesses]
            );

            const executionPayload = await functionInteraction.request({
                authWitnesses: [...authWitnesses],
                fee: { paymentMethod: new SponsoredFeePaymentMethod(sponsoredFPCInstance.address) },
            });

            var sendOptions = await toSendOptions(
                {
                    from: AztecAddress.fromString(request.address),
                    authWitnesses: [...authWitnesses],
                    fee: { paymentMethod: new SponsoredFeePaymentMethod(sponsoredFPCInstance.address) },
                },
            );

            const provenTx = await pxe.proveTx(executionPayload, sendOptions);

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
        try {
            const pkKey = Fr.random();
            const salt = Fr.random();

            const accountContract = new SchnorrAccountContract(deriveSigningKey(pkKey));
            const address = (await getAccountContractAddress(accountContract, pkKey, salt)).toString();

            const TrainContractArtifact = TrainContract.artifact;
            const TokenContractArtifact = TokenContract.artifact;

            const dict: Record<string, string> = {
                "private_key": pkKey.toString(),
                "private_salt": salt.toString(),
            };

            const provider = createAztecNodeClient("https://devnet.aztec-labs.com");

            const l1Contracts = await provider.getL1ContractAddresses();

            const fullConfig = { ...getPXEConfig(), l1Contracts, proverEnabled: true };

            const store = await createStore(address, {
                dataDirectory: this.configService.storePath,
                dataStoreMapSizeKb: 1e6,
            });

            const wallet = await TestWallet.create(provider, fullConfig, { store });

            const sponsoredFPCInstance = await getContractInstanceFromInstantiationParams(
                SponsoredFPCContract.artifact,
                { salt: new Fr(0) },
            );

            const paymentMethod = new SponsoredFeePaymentMethod(sponsoredFPCInstance.address);
            const schnorrAccount = await wallet.createSchnorrAccount(
                pkKey,
                salt,
                deriveSigningKey(pkKey),
            );

            await wallet.registerContract(
                sponsoredFPCInstance,
                SponsoredFPCContract.artifact,
            );

            await (await schnorrAccount.getDeployMethod())
                .send({ from: AztecAddress.ZERO, fee: { paymentMethod } })
                .wait({ timeout: 300000 });

            //register token contract   
            const tokenContractInstanceWithAddress = await provider.getContract(
                AztecAddress.fromString("0x04593cd209ec9cce4c2bf3af9003c262fbda9157d75788f47e45a14db57fac3b")
            );

            await wallet.registerContract({
                instance: tokenContractInstanceWithAddress,
                artifact: TokenContractArtifact,
            });

            //register train contract
            const contractInstanceWithAddress = await provider.getContract(
                AztecAddress.fromString("0x07fbdc90f60f474514ab79c99b50ef27b91ce594c168a38cb1dcadae3244f859")
            );

            await wallet.registerContract({
                instance: contractInstanceWithAddress,
                artifact: TrainContractArtifact,
            });

            //sender rebalance addresses
            await wallet.registerSender(AztecAddress.fromString("0x1c34568017bacdf953140c8a7498ad113ea3fac1dfaf6963928194c85fc3bb2b"));
            await wallet.registerSender(AztecAddress.fromString("0x04030b28dc89132e12478f78e55c4fd4c1454b62fe54dd4a3e749867b58b6d70"));
            await wallet.registerSender(AztecAddress.fromString("0x2fd22a1f24263bed186fc8588027ee40fbfa7f59153eaf586102cf70eb5916b3"));

            await this.privateKeyService.setDictAsync(address.toString(), dict);

            return { address };
        }
        catch (error) {
            throw new BadRequestException(`Error while trying to generate account: ${error.message}`);
        }
    }
}

interface FunctionInteraction {
    interactionAddress: string,
    functionName: string,
    args: any[],
    callerAddress?: string,
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