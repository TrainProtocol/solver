import { Abi, Account, cairo, Call, constants, Contract, hash, RpcProvider, shortString, transaction, TransactionType as StarknetTransactionType, uint256, addAddressPadding } from "starknet";
import { ETransactionVersion2, TypedData, TypedDataRevision } from "starknet-types-07";
import { injectable, inject } from "tsyringe";
import erc20Json from './ABIs/ERC20.json'
import { StarknetPublishTransactionRequest } from "../Models/StarknetPublishTransactionRequest ";
import { BigNumber, utils } from "ethers";
import { InvalidTimelockException } from "../../Blockchain.Abstraction/Exceptions/InvalidTimelockException";
import { PrivateKeyRepository } from "../../Blockchain.Abstraction/Models/WalletsModels/PrivateKeyRepository";
import { ParseNonces } from "./Helper/ErrorParser";
import { CalcV2InvokeTxHashArgs } from "../Models/StarknetTransactioCalculationType";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { TrackBlockEventsAsync } from "./Helper/StarknetEventTracker";
import Redis from "ioredis";
import Redlock from "redlock";
import 'reflect-metadata';
import { validateTransactionStatus } from "./Helper/StarknetTransactionStatusValidator";
import { createLockCallData, createRedeemCallData, createRefundCallData, createAddLockSigCallData, createApproveCallData, createTransferCallData, createCommitCallData } from "./Helper/StarknetTransactionBuilder";
import { BLOCK_WITH_TX_HASHES } from "starknet-types-07/dist/types/api/components";
import { BuildLockKey, BuildNonceKey } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/RedisHelper";
import { TimeSpan } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/TimeSpanConverter";
import { AllowanceRequest } from "../../Blockchain.Abstraction/Models/AllowanceRequest";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee, FixedFeeData } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetBatchTransactionRequest } from "../../Blockchain.Abstraction/Models/GetBatchTransactionRequest";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NextNonceRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { IStarknetBlockchainActivities } from "./IStarknetBlockchainActivities";
import { TransactionStatus } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus";
import { TransactionType } from "../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { TransactionNotComfirmedException } from "../../Blockchain.Abstraction/Exceptions/TransactionNotComfirmedException";
import { DetailedNetworkDto } from "../../Blockchain.Abstraction/Models/DetailedNetworkDto";

@injectable()
export class StarknetBlockchainActivities implements IStarknetBlockchainActivities {
    constructor(
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock
    ) { }

    readonly FeeSymbol = "ETH";
    readonly FeeDecimals = 18;
    readonly FEE_ESTIMATE_MULTIPLIER = BigInt(4);

    public async GetBatchTransaction(request: GetBatchTransactionRequest): Promise<TransactionResponse> {
        let transaction: TransactionResponse = null;

        for (const transactionId of request.TransactionHashes) {
            transaction = await this.GetTransactionByHashAsync(request.network, transactionId);
        }

        if (!transaction) {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        const transaction = await this.GetTransactionByHashAsync(request.network, request.transactionHash);

        if (!transaction) {
            throw new TransactionNotComfirmedException(`Transaction ${request.transactionHash} not found`);
        }

        return transaction;
    }

    private async GetTransactionByHashAsync(network: DetailedNetworkDto, transactionHash: string): Promise<TransactionResponse> {

        const provider = new RpcProvider({ nodeUrl: network.nodes[0].url });

        const statusResponse = await provider.getTransactionStatus(transactionHash);

        const { finality_status, execution_status } = statusResponse;

        const transactionStatus = validateTransactionStatus(finality_status, execution_status);

        if (transactionStatus === TransactionStatus.Failed) {
            throw new TransactionFailedException(`Transaction ${transactionHash} failed with status: ${execution_status}`);
        }

        const transactionReceiptResponse = await provider.getTransactionReceipt(transactionHash);

        const confrimedTransaction = transactionReceiptResponse.isSuccess() ? transactionReceiptResponse : null;

        if (!confrimedTransaction) {
            return null;
        }

        const feeInWei = confrimedTransaction.actual_fee.amount;

        const feeAmount = Number(utils.formatUnits(BigNumber.from(feeInWei), this.FeeDecimals));

        let transactionModel: TransactionResponse = {
            TransactionHash: transactionHash,
            Confirmations: transactionStatus === TransactionStatus.Initiated ? 0 : 1,
            Status: transactionStatus,
            FeeAsset: "ETH",
            FeeAmount: feeAmount,
            Timestamp: new Date(),
            NetworkName: network.name,
        };

        const isConfirmed = "block_number" in confrimedTransaction;

        if (isConfirmed) {
            const blockNumber = confrimedTransaction.block_number as string;
            const blockData = await provider.getBlockWithTxHashes(blockNumber);

            transactionModel.Timestamp = new Date(blockData.timestamp * 1000);
        }

        return transactionModel;
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url,
        });

        const lastBlockNumber = await provider.getBlockNumber();

        const blockData = await provider.getBlockWithTxHashes(lastBlockNumber) as BLOCK_WITH_TX_HASHES;

        return {
            blockNumber: lastBlockNumber,
            blockHash: blockData.block_hash,
        };
    }

    public async GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url
        });

        return TrackBlockEventsAsync(
            request.network,
            provider,
            request.walletAddresses,
            request.fromBlock,
            request.toBlock,
        );
    }

    public async GetNextNonce(request: NextNonceRequest): Promise<string> {
        const provider = new RpcProvider({ nodeUrl: request.network.nodes[0].url });

        const formattedAddress = formatAddress(request.Address);
        const lockKey = BuildLockKey(request.network.displayName, formattedAddress);
        const nonceKey = BuildNonceKey(request.network.displayName, formattedAddress);

        const lock = await this.lockFactory.acquire(
            [lockKey],
            TimeSpan.FromSeconds(25),
            {
                retryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
            }
        );

        try {
            let currentNonce = BigInt(-1);

            const cached = await this.redis.get(nonceKey);
            if (cached !== null) {
                currentNonce = BigInt(cached);
            }

            const nonceHex = await provider.getNonceForAddress(formattedAddress, "pending");
            let nonce = BigInt(nonceHex);

            if (nonce <= currentNonce) {
                nonce = currentNonce + BigInt(1);
            }

            await this.redis.set(nonceKey, nonce.toString(), "EX", TimeSpan.FromDays(7));

            return nonce.toString();
        } finally {
            await lock.release().catch(() => { });
        }
    }

    public async PublishTransaction(request: StarknetPublishTransactionRequest): Promise<string> {
        let result: string;

        const privateKey = await new PrivateKeyRepository().getAsync(request.fromAddress);

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url
        });

        const account = new Account(provider, request.fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(request.callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: request.fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: request.fee.FixedFeeData.FeeInWei,
            chainId: request.network.chainId as constants.StarknetChainId,
            nonce: request.nonce
        };

        const calcualtedTxHash = hash.calculateInvokeTransactionHash(args);

        try {

            const executeResponse = await account.execute(
                [transferCall],
                undefined,
                {
                    maxFee: request.fee.FixedFeeData.FeeInWei,
                    nonce: request.nonce
                },
            );

            result = executeResponse.transaction_hash;

            if (!result || !result.startsWith("0x")) {
                throw new Error(`Withdrawal response didn't contain a correct transaction hash. Response: ${JSON.stringify(executeResponse)}`);
            }

            return result;
        }
        catch (error) {
            const nonceInfo = ParseNonces(error?.message);

            if (nonceInfo && nonceInfo.providedNonce < nonceInfo.expectedNonce) {
                return calcualtedTxHash;
            }

            throw error;
        }
    }

    public async BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
        try {
            switch (request.type) {
                case TransactionType.HTLCLock:
                    return createLockCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCRedeem:
                    return createRedeemCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCRefund:
                    return createRefundCallData(request.network, request.prepareArgs);
                case TransactionType.HTLCAddLockSig:
                    return createAddLockSigCallData(request.network, request.prepareArgs);
                case TransactionType.Approve:
                    return createApproveCallData(request.network, request.prepareArgs);
                case TransactionType.Transfer:
                    return createTransferCallData(request.network, request.prepareArgs);
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
            const token = request.network.tokens.find(t => t.symbol === request.asset);

            if (!token) {
                throw new Error(`Token not found for network ${request.network.name} and asset ${request.asset}`);
            }

            const provider = new RpcProvider({
                nodeUrl: request.network.nodes[0].url
            });

            const erc20 = new Contract(erc20Json as Abi, token.contract, provider);
            const balanceResult = await erc20.balanceOf(request.address);
            const balanceInWei = BigNumber.from(uint256.uint256ToBN(balanceResult.balance as any).toString());

            let result: BalanceResponse = {
                amount: Number(balanceInWei)
            }

            return result;
        }
        catch (error) {
            throw error;
        }
    }

    public async SimulateTransaction(request: StarknetPublishTransactionRequest): Promise<string> {

        const privateKey = await new PrivateKeyRepository().getAsync(request.fromAddress);

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url
        });

        const account = new Account(provider, request.fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(request.callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: request.fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: request.fee.FixedFeeData.FeeInWei,
            chainId: request.network.chainId as constants.StarknetChainId,
            nonce: request.nonce
        };

        const calcualtedTxHash = await hash.calculateInvokeTransactionHash(args);

        try {
            await account.simulateTransaction(
                [
                    {
                        type: StarknetTransactionType.INVOKE,
                        payload: [transferCall]
                    }
                ],
                {
                    nonce: request.nonce
                });

            return calcualtedTxHash;
        }
        catch (error) {
            const nonceInfo = ParseNonces(error?.message);

            if (nonceInfo && nonceInfo.providedNonce > nonceInfo.expectedNonce) {
                throw new Error(`The nonce is too high. Current nonce: ${nonceInfo.providedNonce}, expected nonce: ${nonceInfo.expectedNonce}`);
            }
            else if (nonceInfo && nonceInfo.providedNonce < nonceInfo.expectedNonce) {
                throw new Error(`The nonce is too low. Current nonce: ${nonceInfo.providedNonce}, expected nonce: ${nonceInfo.expectedNonce}`);
            }
            else if (error?.message && error.message.includes("Invalid TimeLock")) {
                throw new InvalidTimelockException("Invalid TimeLock error encountered");
            }

            throw error;
        }
    }

    public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<Fee> {
        try {
            const privateKey = await new PrivateKeyRepository().getAsync(feeRequest.fromAddress);

            const provider = new RpcProvider({
                nodeUrl: feeRequest.network.nodes[0].url
            });

            const account = new Account(provider, feeRequest.fromAddress, privateKey, '1');

            var transferCall: Call = JSON.parse(feeRequest.callData);

            let feeEstimateResponse = await account.estimateFee(transferCall);

            if (!feeEstimateResponse?.suggestedMaxFee) {
                throw new Error(`Couldn't get fee estimation for the transfer. Response: ${JSON.stringify(feeEstimateResponse)}`);
            };

            const feeInWei = BigNumber
                .from(feeEstimateResponse.suggestedMaxFee)
                .mul(this.FEE_ESTIMATE_MULTIPLIER);

            const fixedfeeData: FixedFeeData = {
                FeeInWei: feeInWei.toString(),
            };

            let result: Fee =
            {
                Asset: this.FeeSymbol,
                FixedFeeData: fixedfeeData,
            }

            const balanceResponse = await this.GetBalance({
                    address: feeRequest.fromAddress,
                    network: feeRequest.network,
                asset: this.FeeSymbol
            });

            const amount = feeInWei.add(BigNumber.from(feeRequest.amount));

            if (BigNumber.from(balanceResponse.amount).lt(amount)) {
                throw new Error(`Insufficient balance for fee. Balance: ${balanceResponse.amount}, Fee: ${amount}`);
            }

            return result;
        }
        catch (error: any) {
            if (error?.message && error.message.includes("Invalid TimeLock")) {
                throw new InvalidTimelockException("Invalid TimeLock error encountered");
            }
            throw error;
        }
    }

    public async ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        try {

            const provider = new RpcProvider({
                nodeUrl: request.detailedNetworkDto.nodes[0].url
            });

            const addlockData: TypedData = {
                domain: {
                    name: 'Train',
                    version: shortString.encodeShortString("v1"),
                    chainId: request.detailedNetworkDto.chainId,
                    revision: TypedDataRevision.ACTIVE,
                },
                primaryType: 'AddLockMsg',
                types: {
                    StarknetDomain: [
                        {
                            name: 'name',
                            type: 'shortstring',
                        },
                        {
                            name: 'version',
                            type: 'shortstring',
                        },
                        {
                            name: 'chainId',
                            type: 'shortstring',
                        },
                        {
                            name: 'revision',
                            type: 'shortstring'
                        }
                    ],
                    AddLockMsg: [
                        { name: 'Id', type: 'u256' },
                        { name: 'hashlock', type: 'u256' },
                        { name: 'timelock', type: 'u256' }
                    ],
                },
                message: {
                    Id: cairo.uint256(request.commitId),
                    hashlock: cairo.uint256(request.hashlock),
                    timelock: cairo.uint256(request.timelock),
                },
            }

            return await provider.verifyMessageInStarknet(addlockData, request.signatureArray, request.signerAddress);
        }
        catch (error) {
            throw error;
        }
    }

    public async GetSpenderAllowance(request: AllowanceRequest): Promise<number> {
        try {

            const token = request.network.tokens.find(t => t.symbol === request.asset);

            if (!token) {
                throw new Error(`Token not found for network ${request.network.name} and asset ${request.asset}`);
            }

            const spenderAddress = token.contract
                ? request.network.htlcNativeContractAddress
                : request.network.htlcTokenContractAddress

            const provider = new RpcProvider({ nodeUrl: request.network.nodes[0].url });
            const { abi: tokenAbi } = await provider.getClassAt(token.contract);
            const ercContract = new Contract(tokenAbi, token.contract, provider);
            var response: BigInt = await ercContract.allowance(request.ownerAddress, spenderAddress);

            return Number(utils.formatUnits(response.toString(), token.decimals))
        }
        catch (error) {
            throw error;
        }
    }
}

export function formatAddress(address: string): string {
    return addAddressPadding(address).toLowerCase();
}
