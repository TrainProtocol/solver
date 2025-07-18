import { ContractType } from "../../../Data/Entities/Contracts";
import { AccountType } from "../../../Data/Entities/ManagedAccounts";
import { NodeType } from "../../../Data/Entities/Nodes";
import { SolverContext } from "../../../Data/SolverContext";
import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee, FixedFeeData, LegacyFeeData } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { TransactionStatus } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { inject, injectable } from "tsyringe";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { utils } from "ethers";
import TrackBlockEventsAsync from "./Helper/FuelEventTracker";
import { IAztecBlockchainActivities } from "./IAztecBlockchainActivities";
import { CreateAddLockSigCallData, CreateRefundCallData, CreateLockCallData, CreateRedeemCallData } from "./Helper/FuelTransactionBuilder";
import { FuelPublishTransactionRequest } from "../Models/FuelPublishTransactionRequest";
import { PrivateKeyRepository } from "../../Blockchain.Abstraction/Models/WalletsModels/PrivateKeyRepository";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import abi from "./ABIs/ERC20.json" with { type: 'json' };//with { type: 'json' } this part will change after tsconfig changes
import { AztecAddress, Contract, createAztecNodeClient, FieldsOf, Fr, SponsoredFeePaymentMethod, TxHash, TxReceipt, waitForPXE, } from "@aztec/aztec.js";
import { TokenContract } from '@aztec/noir-contracts.js/Token';
import { getPXEServiceConfig } from "@aztec/pxe/config";
import { createPXEService } from "@aztec/pxe/server";
import { getSchnorrAccount, getSchnorrWallet } from "@aztec/accounts/schnorr";
import { TrainContract } from "./Helper/Train.ts";
import { deriveSigningKey } from '@aztec/stdlib/keys';
import { PXECreationOptions } from '../../../../node_modules/@aztec/pxe/src/entrypoints/pxe_creation_options.ts';
import { createStore } from "@aztec/kv-store/lmdb";
import { getSponsoredFPCInstance } from "./Helper/fpc.ts";
import { SponsoredFPCContract } from "@aztec/noir-contracts.js/SponsoredFPC";

@injectable()
export class AztecBlockchainActivities implements IAztecBlockchainActivities {
    constructor(
        @inject(SolverContext) private dbContext: SolverContext
    ) { }

    public async BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .leftJoinAndSelect("network.contracts", "c")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            switch (request.TransactionType) {
                case TransactionType.HTLCLock:
                    return CreateLockCallData(network, request.Args);
                case TransactionType.HTLCRedeem:
                    return CreateRedeemCallData(network, request.Args);
                case TransactionType.HTLCRefund:
                    return CreateRefundCallData(network, request.Args);
                case TransactionType.HTLCAddLockSig:
                    return CreateAddLockSigCallData(network, request.Args);
                default:
                    throw new Error(`Unknown function name ${request.TransactionType}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async GetBalance(request: BalanceRequest): Promise<BalanceResponse> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            const TokenContractArtifact = TokenContract.artifact;
            const privateKey = Fr.fromString(await new PrivateKeyRepository().getAsync(request.FromAddress));
            const privateSalt = Fr.fromString(await new PrivateKeyRepository().getAsync(request.FromAddress + '1'));//assuming that the salt will store in vault like public address + 1

            const node = network.nodes.find(n => n.type === NodeType.Primary);
            if (!node) {
                throw new Error(`Primary node not found for network ${request.NetworkName}`);
            }

            const token = network.tokens.find(t => t.asset === request.Asset);
            if (!token) {
                throw new Error(`Token not found for network ${request.NetworkName} and asset ${request.Asset}`)
            }

            const provider = createAztecNodeClient(node.url);

            const fullConfig = {
                ...getPXEServiceConfig(),
                l1Contracts: await provider.getL1ContractAddresses(),
            };

            const store = await createStore('PLOR', {
                dataDirectory: 'store',
                dataStoreMapSizeKB: 1e6,
            });

            const options: PXECreationOptions = {
                loggers: {},
                store,
            };

            const pxe = await createPXEService(provider, fullConfig, options);
            await waitForPXE(pxe);

            const schnorrAccount = await getSchnorrAccount(
                pxe,
                privateKey,
                deriveSigningKey(privateKey),
                privateSalt
            );
            await schnorrAccount.register();

            const schnorrWallet = await schnorrAccount.getWallet();
            const tokenContractInstance = await provider.getContract(AztecAddress.fromString(token.tokenContract))

            await pxe.registerContract({
                instance: tokenContractInstance,
                artifact: TokenContractArtifact
            });

            const tokenInstance = await Contract.at(
                AztecAddress.fromString(token.TokenContract),
                TokenContractArtifact,
                schnorrWallet,
            );

            const assetResponse = await tokenInstance.methods
                .balance_of_private(schnorrWallet.getAddress())
                .simulate();

            let result: BalanceResponse =
            {
                Amount: Number(utils.formatUnits(assetResponse, token.decimals)),
                AmountInWei: assetResponse,
                Decimals: token.decimals
            }

            return result;
        }
        catch (error) {
            throw error;
        }
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {
        try {
            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
                .getOne();

            if (!network) {
                throw new Error(`Network ${request.NetworkName} not found`);
            }

            const node = network.nodes.find((n) => n.type === NodeType.Primary);
            if (!node) {
                throw new Error(
                    `Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`
                );
            }

            const provider = createAztecNodeClient(node.url);

            const lastBlockNumber = await provider.getProvenBlockNumber();

            return {
                BlockNumber: lastBlockNumber
            };
        }
        catch (error) {
            throw error;
        }
    }

    public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee> {
        try {
            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.contracts", "c")
                .leftJoinAndSelect("network.tokens", "t")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: feeRequest.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);
            if (!node) {
                throw new Error(`Primary node not found for network ${feeRequest.NetworkName}`);
            }

            const TrainContractArtifact = TrainContract.artifact;
            const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress).address;

            const token = network.tokens.find(t => t.asset === feeRequest.Asset);
            if (!token) {
                throw new Error(`Token not found for network ${network.name} and asset ${feeRequest.Asset}`);
            }
            const TokenContractArtifact = TokenContract.artifact;
            const privateKey = Fr.fromString(await new PrivateKeyRepository().getAsync(feeRequest.FromAddress));
            const privateSalt = Fr.fromString(await new PrivateKeyRepository().getAsync(feeRequest.FromAddress + '1'));//assuming that the salt will store in vault like public address + 1
            const provider = createAztecNodeClient(node.url);

            const fullConfig = {
                ...getPXEServiceConfig(),
                l1Contracts: await provider.getL1ContractAddresses(),
            };

            const store = await createStore('PLOR', {
                dataDirectory: 'store',
                dataStoreMapSizeKB: 1e6,
            });

            const options: PXECreationOptions = {
                loggers: {},
                store,
            };

            const pxe = await createPXEService(provider, fullConfig, options);
            await waitForPXE(pxe);

            const schnorrAccount = await getSchnorrAccount(
                pxe,
                privateKey,
                deriveSigningKey(privateKey),
                privateSalt
            );
            await schnorrAccount.register();

            const schnorrWallet = await schnorrAccount.getWallet();
            const tokenContractInstance = await provider.getContract(AztecAddress.fromString(token.tokenContract))
            const htlcContractInstance = await provider.getContract(AztecAddress.fromString(htlcContractAddress))

            await pxe.registerContract({
                instance: tokenContractInstance,
                artifact: TokenContractArtifact,
            });

            const tokenInstance = await Contract.at(
                AztecAddress.fromString(token.TokenContract),
                TokenContractArtifact,
                schnorrWallet,
            );

            await pxe.registerContract({
                instance: htlcContractInstance,
                artifact: TrainContractArtifact,
            })

            const htlcConstractInstance = await Contract.at(
                AztecAddress.fromString(token.TokenContract),
                TokenContractArtifact,
                schnorrWallet,
            );

            if (feeRequest.callData.functionName == "lock_private_solver") {

                const amount = Number(utils.parseUnits(feeRequest.Amount.toString(), token.decimals))
                const transfer = tokenInstance
                    .withWallet(schnorrWallet)
                    .methods.transfer_to_public(
                        schnorrWallet.getAddress(),
                        htlcContractInstance.address,
                        amount,
                        feeRequest.callData.functionParams[6],//randomness
                    );

                const witness = await schnorrWallet.createAuthWit({
                    caller: htlcConstractInstance.address,
                    action: transfer,
                });

                const estimateTx = await htlcConstractInstance.methods[feeRequest.functionName](...feeRequest.callData.functionParams).estimateGas({ authWitnesses: [witness] });
            }
            else {
                const estimateTx = await htlcConstractInstance.methods[feeRequest.functionName](...feeRequest.callData.functionParams).estimateGas();
            }

            const fixedfeeData: FixedFeeData = {
                FeeInWei: '',
            };

            const legacyFeeData: LegacyFeeData = {
                GasLimit: '',
                GasPriceInWei: '',
                L1FeeInWei: null
            };

            const feeToken = network.tokens.find(t => t.isNative === true);
            const result: Fee = {
                Asset: feeToken.asset,
                Decimals: feeToken.decimals,
                FixedFeeData: fixedfeeData,
                LegacyFeeData: legacyFeeData,
            }

            return result;
        }
        catch (error: any) {
            if (error?.message && (error.message.includes("Invalid Reward Timelock") || error.message.includes("No Future Timelock"))) {
                throw new Error;
            }
            throw error;
        }
    }

    public async ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        return true;
    }

    public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .leftJoinAndSelect("network.contracts", "c")
            .leftJoinAndSelect("network.managedAccounts", "ma")
            .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
            .getOne();

        if (!network) {
            throw new Error(`Network ${request.NetworkName} not found`);
        }

        const node = network.nodes.find(n => n.type === NodeType.Primary);
        if (!node) {
            throw new Error(`Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`);
        }

        const solverAddress = network.managedAccounts.find(m => m.type === AccountType.Primary)?.address;
        const htlcAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)?.address;

        const tokens = await this.dbContext.Tokens
            .createQueryBuilder("currencies")
            .leftJoinAndSelect("currencies.network", "n")
            .getMany();

        const result = await TrackBlockEventsAsync(network.name, node.url, request.FromBlock, request.ToBlock, htlcAddress, solverAddress, tokens);

        return result;
    }

    public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        let transactionResponse: TransactionResponse;
        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.tokens", "t")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
            .getOne();

        if (!network) {
            throw new Error(`Network ${request.NetworkName} not found`);
        }

        const node = network.nodes.find((n) => n.type === NodeType.Primary);
        if (!node) {
            throw new Error(
                `Node with type ${NodeType.Primary} is not configured in ${request.NetworkName}`
            );
        }

        const nativeToken = network.tokens.find(t => t.isNative === true);
        const provider = createAztecNodeClient(node.url);

        const transaction = await provider.getTxReceipt(TxHash.fromString(request.TransactionHash));

        if (transaction.status != 'success') {
            throw new TransactionFailedException(`Transaction ${request.TransactionHash} failed on network ${network.name}`);
        }

        const transactionBlock = await provider.getBlock(transaction.blockNumber);
        const latestblock = await provider.getProvenBlockNumber();
        const timestamp = transactionBlock.header.globalVariables.timestamp.toBigInt().toString();
        const confirmations = latestblock - transactionBlock.number;

        transactionResponse = {
            NetworkName: network.name,
            TransactionHash: request.TransactionHash,
            Confirmations: confirmations,
            Timestamp: timestamp,
            FeeAmount: Number(utils.formatUnits(transaction.transactionFee.toString(), nativeToken.decimals)),
            FeeAsset: nativeToken.asset,
            Status: transaction.status,
        }

        return transactionResponse;
    }

    public async PublishTransaction(request: FuelPublishTransactionRequest): Promise<string> {

        let result: string;
        let tx: FieldsOf<TxReceipt>;

        try {
            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.contracts", "c")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${request.NetworkName}`);
            }

            const TrainContractArtifact = TrainContract.artifact;
            const TokenContractArtifact = TokenContract.artifact;

            const htlcContractAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress).address;

            const token = network.tokens.find(t => t.asset === request.Asset);
            if (!token) {
                throw new Error(`Token not found for network ${network.name} and asset ${request.Asset}`);
            }
            const privateKey = Fr.fromString(await new PrivateKeyRepository().getAsync(request.FromAddress));
            const privateSalt = Fr.fromString(await new PrivateKeyRepository().getAsync(request.FromAddress + '1'));//assuming that the salt will store in vault like public address + 1

            const provider = createAztecNodeClient(node.url);
            const fullConfig = {
                ...getPXEServiceConfig(),
                l1Contracts: await provider.getL1ContractAddresses(),
            };

            const store = await createStore('PLOR', {
                dataDirectory: 'store',
                dataStoreMapSizeKB: 1e6,
            });
            const options: PXECreationOptions = {
                loggers: {},
                store,
            };

            const pxe = await createPXEService(provider, fullConfig, options);
            await waitForPXE(pxe);

            const schnorrAccount = await getSchnorrAccount(
                pxe,
                privateKey,
                deriveSigningKey(privateKey),
                privateSalt
            );
            await schnorrAccount.register();

            const schnorrWallet = await schnorrAccount.getWallet();

            const tokenContractInstance = await provider.getContract(AztecAddress.fromString(token.tokenContract))
            await pxe.registerContract({
                instance: tokenContractInstance,
                artifact: TokenContractArtifact,
            });
            const tokenInstance = await Contract.at(
                AztecAddress.fromString(token.TokenContract),
                TokenContractArtifact,
                schnorrWallet,
            );

            const htlcContractInstanceWithAddress = await provider.getContract(AztecAddress.fromString(htlcContractAddress))
            await pxe.registerContract({
                instance: htlcContractInstanceWithAddress,
                artifact: TrainContractArtifact,
            })
            const htlcContract = await Contract.at(
                AztecAddress.fromString(token.TokenContract),
                TokenContractArtifact,
                schnorrWallet,
            );

            //In the testnet environment, we use sponsoredFPC to cover fees instead of performing manual fee estimation.
            const sponsoredFPC = await getSponsoredFPCInstance();
            const paymentMethod = new SponsoredFeePaymentMethod(sponsoredFPC.address);
            await pxe.registerContract({
                instance: sponsoredFPC,
                artifact: SponsoredFPCContract.artifact,
            });

            if (request.callData.functionName == "lock_private_solver") {

                const amount = Number(utils.parseUnits(request.Amount.toString(), token.decimals))
                const transfer = tokenInstance
                    .withWallet(schnorrWallet)
                    .methods.transfer_to_public(
                        schnorrWallet.getAddress(),
                        htlcContractInstanceWithAddress.address,
                        amount,
                        request.callData.functionParams[6],//randomness
                    );

                const witness = await schnorrWallet.createAuthWit({
                    caller: htlcContractInstanceWithAddress.address,
                    action: transfer,
                });

                tx = await htlcContract.methods[request.functionName](...request.callData.functionParams)
                    .send({ authWitnesses: [witness], fee: { paymentMethod } })
                    .wait({ timeout: 1200000 });

                result = tx.txHash.toString();
            }
            else {
                tx = await htlcContract.methods[request.functionName](...request.callData.functionParams)
                    .send({ fee: { paymentMethod } })
                    .wait({ timeout: 1200000 });

                result = tx.txHash.toString();
            }
        }
        catch (error) {
            throw error;
        }

        return result;
    }
}