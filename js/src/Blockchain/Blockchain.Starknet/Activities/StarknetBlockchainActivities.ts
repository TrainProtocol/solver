import { 
    Uint256,
    Abi, cairo, Call, Contract,
    hash, RpcProvider, shortString, uint256, addAddressPadding,
    Invocation, SuccessfulTransactionReceiptResponse, InvocationsSignerDetails,
    stark, AccountInvocations, TransactionType as StarknetTransactionType, v3hash, 
    WeierstrassSignatureType,
    Account,
    TypedData,
    TypedDataRevision,
    AccountInvocationItem,
    V3InvocationsSignerDetails,
    CallData,
    RawArgs,
    transaction} 
    from "starknet";
import { injectable, inject } from "tsyringe";
import erc20Json from './ABIs/ERC20.json'
import { PublishTransactionRequest} from "../Models/TransactionModels";
import { BigNumber, utils } from "ethers";
import { InvalidTimelockException } from "../../Blockchain.Abstraction/Exceptions/InvalidTimelockException";
import { ParseNonces } from "./Helper/ErrorParser";
import { TransactionFailedException } from "../../Blockchain.Abstraction/Exceptions/TransactionFailedException";
import { TrackBlockEventsAsync } from "./Helper/StarknetEventTracker";
import Redis from "ioredis";
import Redlock from "redlock";
import { validateTransactionStatus } from "./Helper/StarknetTransactionStatusValidator";
import { createLockCallData, createRedeemCallData, createRefundCallData, createAddLockSigCallData, createApproveCallData, createTransferCallData, createCommitCallData } from "./Helper/StarknetTransactionBuilder";
import { buildLockKey, buildCurrentNonceKey } from "../../Blockchain.Abstraction/Infrastructure/RedisHelper/RedisHelper";
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
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
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
import { EnsureSufficientBalanceRequest } from "../Models/EnsureSufficientBalanceModels";
import { ComposeRawTransactionRequest, ComposeRawTransactionResponse } from "../Models/ComposeRawTxModels";
import { SignTransactionRequest } from "../Models/SignTransactionRequest";
import { InvokeTransactionV3, sendInvocation } from "./Helper/Client";
import { TreasuryClient } from "../../Blockchain.Abstraction/Infrastructure/TreasuryClient/treasuryClient";
import { StarknetFeeModel } from "../Models/StarknetFeeModel";
import { resourceBoundsToHexString, signatureToHexArray, toHex } from "./Helper/Utils";

@injectable()
export class StarknetBlockchainActivities implements IStarknetBlockchainActivities {
    constructor(
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock,
    ) { }

    readonly FEE_ESTIMATE_MULTIPLIER = BigInt(4);

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
                amount: balanceInWei.toString()
            }

            return result;
        }
        catch (error) {
            throw error;
        }
    }

    public async GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse> {

        const transaction = await this.GetTransactionByHashAsync(request.network, request.transactionHash);

        if (!transaction) {
            throw new TransactionNotComfirmedException(`Transaction ${request.transactionHash} not found`);
        }

        return transaction;
    }

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

    private async GetTransactionByHashAsync(network: DetailedNetworkDto, transactionHash: string): Promise<TransactionResponse> {

        const provider = new RpcProvider({ nodeUrl: network.nodes[0].url });

        const statusResponse = await provider.getTransactionStatus(transactionHash);

        const { finality_status, execution_status } = statusResponse;

        const transactionStatus = validateTransactionStatus(finality_status, execution_status);

        if (transactionStatus === TransactionStatus.Failed) {
            throw new TransactionFailedException(`Transaction ${transactionHash} failed with status: ${execution_status}`);
        }

        const transactionReceiptResponse = await provider.getTransactionReceipt(transactionHash);

        const confrimedTransaction = transactionReceiptResponse.value as SuccessfulTransactionReceiptResponse

        if (!confrimedTransaction) {
            return null;
        }

        const blockData = await provider.getBlockWithTxHashes(confrimedTransaction.block_number);

        const feeAmount = confrimedTransaction.actual_fee.amount;

        let transactionModel: TransactionResponse = {
            transactionHash: transactionHash,
            decimals: network.nativeToken.decimals,
            feeDecimals: network.nativeToken.decimals,
            confirmations: transactionStatus === TransactionStatus.Initiated ? 0 : 1,
            status: transactionStatus,
            feeAsset: network.nativeToken.symbol,
            feeAmount: Number(feeAmount).toString(),
            timestamp: new Date(blockData.timestamp * 1000),
            networkName: network.name
        };

        return transactionModel;
    }

    public async GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url,
        });

        const lastBlockNumber = await provider.getBlockNumber();

        const blockData = await provider.getBlockWithTxHashes(lastBlockNumber);

        const isConfirmed = "block_hash" in blockData;
        if (isConfirmed) {
            return {
                blockNumber: lastBlockNumber,
                blockHash: blockData.block_hash,
            };
        }
        else {
            throw new Error("Block doesn't confirmed")
        }
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

        const formattedAddress = formatAddress(request.address);
        const lockKey = buildLockKey(request.network.name, formattedAddress);
        const nonceKey = buildCurrentNonceKey(request.network.name, formattedAddress);

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

    public async PublishTransaction(request: PublishTransactionRequest): Promise<string> {
        let invocation : any = JSON.parse(request.signedRawData);
        let signatureString = JSON.stringify(invocation.signature);

        const signature = this.deserializeWithBigInt(signatureString) as WeierstrassSignatureType;

        invocation.signature = signature;
        const signerDetails: V3InvocationsSignerDetails = JSON.parse(request.signerInvocationDetails);
        const a = JSON.stringify(invocation.calldata);

        var data : Call = 
        {
            contractAddress: invocation.contractAddress,
            entrypoint: invocation.entrypoint,
            calldata: invocation.calldata
        };

        const calld = transaction.getExecuteCalldata([data], signerDetails.cairoVersion);

        const accountInvocations: InvokeTransactionV3 = 
        {
            type: "INVOKE",
            sender_address: request.fromAddress,
            calldata: CallData.toHex(calld),
            version: signerDetails.version,
            signature: signatureToHexArray(invocation.signature),
            nonce: toHex(signerDetails.nonce),
            resource_bounds: resourceBoundsToHexString(signerDetails.resourceBounds),
            tip: toHex(signerDetails.tip),
            paymaster_data: [],
            account_deployment_data: [],
            fee_data_availability_mode: signerDetails.feeDataAvailabilityMode,
            nonce_data_availability_mode: signerDetails.nonceDataAvailabilityMode
        };

        const nodeUrl = request.network.nodes[0].url;

        const txHash = await sendInvocation(nodeUrl, accountInvocations);

        return txHash;
    }

    public async SimulateTransaction(request: PublishTransactionRequest): Promise<void> {

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url
        });

        let invocation: Invocation = this.deserializeWithBigInt(request.signedRawData);

        const signature = invocation.signature as WeierstrassSignatureType;

        invocation.signature = signature;
        const signerDetails: InvocationsSignerDetails = JSON.parse(request.signerInvocationDetails);

        const accountInvocations: AccountInvocations =
            [
                {
                    type: StarknetTransactionType.INVOKE,
                    contractAddress: invocation.contractAddress,
                    entrypoint: invocation.entrypoint,
                    nonce: request.nonce,
                    signature: invocation.signature,
                    ...stark.v3Details(signerDetails),
                }
            ];

        try {
            await provider.getSimulateTransaction(accountInvocations);

            return;
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

    public async EstimateFee(feeRequest: EstimateFeeRequest): Promise<StarknetFeeModel> {
        try {

            const provider = new RpcProvider({
                nodeUrl: feeRequest.network.nodes[0].url
            });

            const account = new Account(provider, feeRequest.fromAddress, "0x0");

            var transferCall: Call = JSON.parse(feeRequest.callData);

            var fees = await account.estimateFee([transferCall]);

            // const signature = JSON.parse(feeRequest.signature).signature;
            // const r = uint256.uint256ToBN(signature.r);
            // const recovery = signature.recovery;
            // const s = uint256.uint256ToBN(signature.s);

            // transferCall.signature = {r, s, recovery} as WeierstrassSignatureType;

            // const feeEstimateResponse = await provider.getInvokeEstimateFee(
            //     transferCall,
            //     {
            //         nonce: feeRequest.nonce
            //     }
            // );

            if (!fees?.suggestedMaxFee) {
                throw new Error(`Couldn't get fee estimation for the transfer. Response: ${JSON.stringify(fees)}`);
            };

            const feeInWei = BigNumber
                .from(fees.suggestedMaxFee)
                .mul(this.FEE_ESTIMATE_MULTIPLIER);

            const fixedfeeData: FixedFeeData = {
                FeeInWei: feeInWei.toString(),
            };

            const result: StarknetFeeModel =
            {
                Asset: feeRequest.network.nativeToken.symbol,
                FixedFeeData: fixedfeeData,
                ResourceBounds: fees.resourceBounds
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

    public async EnsureSufficientBalance(request: EnsureSufficientBalanceRequest): Promise<void> {

        const nativeTokenAsset = request.network.nativeToken.symbol;

        const nativeTokenBalance = await this.GetBalance({
            address: request.address,
            network: request.network,
            asset: nativeTokenAsset
        });

        const nativeTokenAmount = BigNumber.from(nativeTokenBalance.amount.toString());
        const amount = BigNumber.from(request.amount);
        const feeAmount = BigNumber.from(request.feeAmount);

        if (nativeTokenAsset === request.asset) {

            if (nativeTokenAmount.lt(amount.add(feeAmount))) {
                throw new Error(`Insufficient balance for fee. Balance: ${nativeTokenBalance.amount} asset ${nativeTokenAsset}, Fee: ${amount.add(feeAmount)}`);
            }
        }
        else {
            const tokenBalance = await this.GetBalance({
                address: request.address,
                network: request.network,
                asset: request.asset
            });

            if (BigNumber.from(nativeTokenBalance.amount).lt(feeAmount)) {
                throw new Error(`Insufficient balance for fee. Balance: ${nativeTokenBalance.amount} asset ${nativeTokenAsset}, Fee: ${feeAmount}`);
            }

            if (BigNumber.from(tokenBalance.amount).lt(amount)) {
                throw new Error(`Insufficient balance for fee. Balance: ${tokenBalance.amount} asset ${request.asset}, Fee: ${amount}`);
            }
        }
    }

    public async ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        try {

            const provider = new RpcProvider({
                nodeUrl: request.network.nodes[0].url
            });

            const addlockData: TypedData = {
                domain: {
                    name: 'Train',
                    version: shortString.encodeShortString("v1"),
                    chainId: request.network.chainId,
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

            return Number(response.toString())
        }
        catch (error) {
            throw error;
        }
    }

    public async ComposeRawTransaction(request: ComposeRawTransactionRequest): Promise<ComposeRawTransactionResponse> {

        const provider = new RpcProvider({
            nodeUrl: request.network.nodes[0].url
        });

        const chainId = await provider.getChainId();

        const signerDetails: InvocationsSignerDetails = {
            ...stark.v3Details({ resourceBounds: request.resourceBounds, tip: 0 }),
            walletAddress: request.address,
            nonce: request.nonce,
            version: "0x3",
            chainId,
            cairoVersion: '1',
            skipValidate: true,
        };

        const parsedInvocation: Invocation = JSON.parse(request.callData);

        let transferCall: Call =
        {
            contractAddress: parsedInvocation.contractAddress,
            entrypoint: parsedInvocation.entrypoint,
            calldata: parsedInvocation.calldata
        };

        const result: ComposeRawTransactionResponse =
        {
            signerInvocationDetails: JSON.stringify(signerDetails),
            unsignedTxn: JSON.stringify(transferCall)
        };

        return result;
    };

    public async SignTransaction(request: SignTransactionRequest): Promise<string> {
        const treasuryClient = new TreasuryClient(request.signerAgentUrl);

        const response = await treasuryClient.signTransaction(request.networkType, request.signRequest);

        return response.signedTxn;
    }

    private deserializeWithBigInt(json: string): any {
        return JSON.parse(json, (_key, value) => {
            // Detect if this value is a uint256-like object
            if (
            value &&
            typeof value === "object" &&
            "low" in value &&
            "high" in value &&
            Object.keys(value).length === 2
            ) {
            try {
                // Convert back to bigint
                return uint256.uint256ToBN(value);
            } catch {
                // If it's not a valid uint256, return as-is
                return value;
            }
            }
            return value;
        });
    }
}

export function formatAddress(address: string): string {
    return addAddressPadding(address).toLowerCase();
}
