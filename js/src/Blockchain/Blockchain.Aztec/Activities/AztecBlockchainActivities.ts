import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { IAztecBlockchainActivities } from "./IAztecBlockchainActivities";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import TrackBlockEventsAsync from "./Helper/AztecEventTracker";
import { createRefundCallData, createLockCallData, createRedeemCallData, createCommitCallData } from "./Helper/AztecTransactionBuilder";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { Tx, TxHash } from "@aztec/aztec.js/tx";
import { mapAztecStatusToInternal } from "./Helper/AztecTransactionStatusMapper";
import { AztecPublishTransactionRequest } from "../Models/AztecPublishTransactionRequest";
import { AztecSignTransactionRequestModel } from "./Models/AztecSignTransactionModel";
import { buildLockKey as buildLockKey, buildCurrentNonceKey, buildNextNonceKey } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/RedisHelper";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { CurrentNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/CurrentNonceRequest";
import { inject, injectable } from "tsyringe";
import Redis from "ioredis";
import Redlock from "redlock";
import { TimeSpan } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/TimeSpanConverter";
import { TransactionNotComfirmedException } from "../../Blockchain.Abstraction/Exceptions/TransactionNotComfirmedException";
import { TrainContract } from "./Helper/Train";
import { PrivateKeyService } from '../KeyVault/vault.service';
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
import { AztecFunctionInteractionModel } from "./Models/AztecFunctionInteractionModel";
import { AztecConfigService } from "../KeyVault/aztec.config";

@injectable()
export class AztecBlockchainActivities implements IAztecBlockchainActivities {
    constructor(
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock,
        @inject("PrivateKeyService") private privateKeyService: PrivateKeyService,
        @inject("AztecConfigService") private aztecConfigService: AztecConfigService,
    ) { }

    public async BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
        try {
            switch (request.type) {
                case TransactionType.HTLCLock:
                    return createLockCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCRedeem:
                    return createRedeemCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCRefund:
                    return createRefundCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCCommit:
                    return createCommitCallData(request.network, request.prepareArgs);
                default:
                    throw new Error(`Unknown function name ${request.type}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async GetBalance(request: BalanceRequest): Promise<BalanceResponse> {

        try {
            const privateKey = await this.privateKeyService.getAsync(request.address);
            const privateSalt = await this.privateKeyService.getAsync(request.address, "private_salt");
            const provider: AztecNode = createAztecNodeClient(request.network.nodes[0].url);
            const l1Contracts = await provider.getL1ContractAddresses();

            const fullConfig = { ...getPXEConfig(), l1Contracts, proverEnabled: true };

            const store = await createStore(request.address, {
                dataDirectory: this.aztecConfigService.storePath,
                dataStoreMapSizeKb: 1e6,
            });

            const token = request.network.tokens.find(t => t.symbol === request.asset);

            const pxe = await TestWallet.create(provider, fullConfig, { store });

            const userAccount = await pxe.createSchnorrAccount(
                Fr.fromString(privateKey),
                Fr.fromString(privateSalt),
                deriveSigningKey(Fr.fromString(privateKey)),
            );

            const tokenInstance = await provider.getContract(AztecAddress.fromString(token.contract));
            await pxe.registerContract(tokenInstance, TokenContract.artifact)

            const tokenContract = await TokenContract.at(AztecAddress.fromString(token.contract), pxe);

            const amount = await tokenContract.methods.balance_of_private(userAccount.address).simulate({ from: userAccount.address });

            return { amount: amount.toString() };
        }
        catch (error) {
            throw new Error(`Error while getting balance: ${error.message}`);
        }
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);
        const lastBlockNumber = await provider.getBlockNumber();
        const blockHash = await (await provider.getBlock(lastBlockNumber)).hash();

        return {
            blockNumber: lastBlockNumber,
            blockHash: blockHash.toString()
        };
    }

    public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

        const result = await TrackBlockEventsAsync(request.network, request.fromBlock, request.toBlock, request.walletAddresses);
        return result;
    }

    public async getTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        const provider = createAztecNodeClient(request.network.nodes[0].url);
        const transaction = await provider.getTxReceipt(TxHash.fromString(request.transactionHash));
        const transactionStatus = mapAztecStatusToInternal(transaction.status)

        if (transactionStatus == TransactionStatus.Initiated) {
            throw new TransactionNotComfirmedException(`Transaction ${request.transactionHash} is still pending on network ${request.network.name}`);
        }

        if (transactionStatus == TransactionStatus.Failed) {
            throw new TransactionFailedException(`Transaction ${request.transactionHash} failed on network ${request.network.name}`);
        }

        const transactionBlock = await provider.getBlock(transaction.blockNumber);
        const latestblock = await provider.getProvenBlockNumber();
        const timestamp = transactionBlock.header.globalVariables.timestamp;
        const confirmations = latestblock - transactionBlock.number;

        const transactionResponse: TransactionResponse = {
            decimals: request.network.nativeToken.decimals,
            networkName: request.network.name,
            transactionHash: request.transactionHash,
            confirmations: confirmations,
            timestamp: new Date(Number(timestamp) * 1000),
            feeAmount: "0",
            feeAsset: request.network.nativeToken.symbol,
            feeDecimals: request.network.nativeToken.decimals,
            status: transactionStatus,
        }

        return transactionResponse;
    }

    public async signTransaction(request: AztecSignTransactionRequestModel): Promise<string> {
        try {
            const privateKey = await this.privateKeyService.getAsync(request.solverAddress);
            const privateSalt = await this.privateKeyService.getAsync(request.solverAddress, "private_salt");
            const provider: AztecNode = createAztecNodeClient(request.nodeUrl);
            const l1Contracts = await provider.getL1ContractAddresses();

            const fullConfig = { ...getPXEConfig(), l1Contracts, proverEnabled: true };

            const accountContract = new SchnorrAccountContract(deriveSigningKey(Fr.fromString(privateKey)));
            const solverAddress = (await getAccountContractAddress(accountContract, Fr.fromString(privateKey), Fr.fromString(privateSalt))).toString();

            const store = await createStore(request.solverAddress, {
                dataDirectory: this.aztecConfigService.storePath,
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

            const solverAccount = await pxe.createSchnorrAccount(
                Fr.fromString(privateKey),
                Fr.fromString(privateSalt),
                deriveSigningKey(Fr.fromString(privateKey)),
            );

            const contractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(request.contractAddress));
            await pxe.registerContract(contractInstanceWithAddress, TrainContract.artifact);

            const tokenInstance = await provider.getContract(AztecAddress.fromString(request.tokenContract));
            await pxe.registerContract(tokenInstance, TokenContract.artifact)

            const contractFunctionInteraction: AztecFunctionInteractionModel = JSON.parse(request.unsignedTxn);
            let authWitnesses: AuthWitness[] = [];

            if (contractFunctionInteraction.authwiths) {
                for (const authWith of contractFunctionInteraction.authwiths) {
                    const requestContractClass = await provider.getContract(AztecAddress.fromString(authWith.interactionAddress));
                    const contractClassMetadata = await pxe.getContractClassMetadata(requestContractClass.currentContractClassId, true);

                    if (!contractClassMetadata.artifact) {
                        throw new Error(`Artifact not registered`);
                    }

                    const functionAbi = getFunctionAbi(contractClassMetadata.artifact, authWith.functionName);

                    if (!functionAbi) {
                        throw new Error("Unable to get function ABI");
                    }

                    authWith.args.unshift(solverAccount.address);

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
                        solverAccount.address,
                        intent,
                    );

                    authWitnesses.push(witness);
                }
            }

            const requestcontractClass = await provider.getContract(AztecAddress.fromString(contractFunctionInteraction.interactionAddress))
            const contractClassMetadata = await pxe.getContractClassMetadata(requestcontractClass.currentContractClassId, true)

            if (!contractClassMetadata.artifact) {
                throw new Error(`Artifact not registered`);
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
                    from: solverAccount.address,
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

            return signedTxn;
        }
        catch (error) {
            throw new Error(`Error while signing transaction: ${error.message}`);
        }
    }

    public async publishTransaction(request: AztecPublishTransactionRequest): Promise<string> {

        const parsed = JSON.parse(request.signedTx);
        const buf = Buffer.from(parsed.signedTx, "hex");
        const signedTx = Tx.fromBuffer(buf);

        const provider = createAztecNodeClient(request.network.nodes[0].url);

        await provider.sendTx(signedTx);
        const txHash = signedTx.getTxHash().toString();

        return txHash;
    }

    public async getNextNonce(request: NextNonceRequest): Promise<number> {
        const lockKey = buildLockKey(request.network.name, request.address);

        const nextNonceKey = buildNextNonceKey(request.network.name, request.address);

        const lock = await this.lockFactory.acquire(
            [lockKey],
            TimeSpan.FromSeconds(25),
            {
                retryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
            });

        try {
            let pendingNonce: number = 0;

            const cached = await this.redis.get(nextNonceKey);

            if (cached !== null) {
                pendingNonce = Number(cached);
            }

            await this.redis.set(nextNonceKey, (pendingNonce + 1).toString(), "EX", TimeSpan.FromDays(7));

            return pendingNonce

        } finally {
            await lock.release().catch(() => { });
        }
    }

    public async checkCurrentNonce(request: CurrentNonceRequest): Promise<void> {
        const currentNonceKey = buildCurrentNonceKey(request.network.name, request.address);

        const cached = await this.redis.get(currentNonceKey);

        let addressCurrentNonce: number = 0;

        if (cached !== null) {
            addressCurrentNonce = Number(cached);
        }

        if (addressCurrentNonce !== request.currentNonce) {
            throw new Error(`Current nonce ${addressCurrentNonce} transaction nonce ${request.currentNonce}`)
        }
    }

    public async updateCurrentNonce(request: CurrentNonceRequest): Promise<void> {
        const lockKey = buildLockKey(request.network.name, request.address);

        const currentNonceKey = buildCurrentNonceKey(request.network.name, request.address);

        const lock = await this.lockFactory.acquire(
            [lockKey],
            TimeSpan.FromSeconds(25),
            {
                retryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
            });

        try {
            await this.redis.set(currentNonceKey, (request.currentNonce + 1).toString(), "EX", TimeSpan.FromDays(7));
        }
        finally {
            await lock.release().catch(() => { });
        }
    }

    ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        throw new Error("Method not implemented.");
    }
}

function getFunctionAbi(
    artifact: ContractArtifact,
    fnName: string,
): FunctionAbi | undefined {
    const fn = getAllFunctionAbis(artifact).find(({ name }) => name === fnName);
    if (!fn) { }
    return fn;
}
