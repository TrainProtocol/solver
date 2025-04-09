import { Abi, Account, cairo, Call, CallData, constants, Contract, hash, RpcProvider, shortString, transaction, TransactionType as StarknetTransactionType, uint256, addAddressPadding } from "starknet";
import { ETransactionVersion2, TypedData, TypedDataRevision } from "starknet-types-07";
import { injectable, inject } from "tsyringe";
import erc20Json from './ABIs/ERC20.json'
import { StarknetPublishTransactionRequest } from "../Models/StarknetPublishTransactionRequest ";
import { BigNumber, utils } from "ethers";
import { AllowanceRequest } from "../../../CoreAbstraction/Models/AllowanceRequest";
import { BalanceRequest } from "../../../CoreAbstraction/Models/BalanceRequestModels/BalanceRequest";
import { EstimateFeeRequest } from "../../../CoreAbstraction/Models/FeesModels/EstimateFeeRequest";
import { AddLockSignatureRequest } from "../../../CoreAbstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { TransactionBuilderRequest } from "../../../CoreAbstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../../CoreAbstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { ContractType } from "../../../Data/Entities/Contracts";
import { NodeType } from "../../../Data/Entities/Nodes";
import { SolverContext } from "../../../Data/SolverContext";
import { InvalidTimelockException } from "../../../Exceptions/InvalidTimelockException";
import { PrivateKeyRepository } from "../../../lib/PrivateKeyRepository";
import { ParseNonces } from "./Helper/ErrorParser";
import { CalcV2InvokeTxHashArgs } from "../Models/StarknetTransactioCalculationType";
import { TransactionType } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionType";
import { Fee, FixedFeeData } from "../../../CoreAbstraction/Models/FeesModels/Fee";
import { IStarknetBlockchainActivities } from "./IStarknetBlockchainActivities";
import { BalanceResponse } from "../../../CoreAbstraction/Models/BalanceRequestModels/BalanceResponse";
import { GetBatchTransactionRequest } from "../../../CoreAbstraction/Models/GetBatchTransactionRequest";
import { GetTransactionRequest } from "../../../CoreAbstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../../CoreAbstraction/Models/ReceiptModels/TransactionResponse";
import { BaseRequest } from "../../../CoreAbstraction/Models/BaseRequest";
import { BlockNumberResponse } from "../../../CoreAbstraction/Models/BlockNumberResponse";
import { EventRequest } from "../../../CoreAbstraction/Models/EventRequest";
import { HTLCBlockEventResponse } from "../../../CoreAbstraction/Models/EventModels/HTLCBlockEventResposne";
import { NextNonceRequest } from "../../../CoreAbstraction/Models/NextNonceRequest";
import { BLOCK_WITH_TX_HASHES } from "starknet-types-07/dist/types/api/components";
import { TransactionStatus } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionStatus";
import { TransactionFailedException } from "../../../Exceptions/TransactionFailedException";
import { Networks } from "../../../Data/Entities/Networks";
import { TransactionNotComfirmedException } from "../../../Exceptions/TransactionNotComfirmedException";
import { AccountType, ManagedAccounts } from "../../../Data/Entities/ManagedAccounts";
import { TrackBlockEventsAsync } from "./Helper/StarknetEventTracker";
import Redis from "ioredis";
import Redlock from "redlock";
import { validateTransactionStatus } from "./Helper/StarknetTransactionStatusValidator";
import { CreateLockCallData, CreateRedeemCallData, CreateRefundCallData, CreateAddLockSigCallData, CreateApproveCallData, CreateTransferCallData } from "./Helper/StarknetTransactionBuilder";

@injectable()
export class StarknetBlockchainActivities implements IStarknetBlockchainActivities {constructor(
    @inject(SolverContext) private dbContext: SolverContext,
    @inject("Redis") private redis: Redis,
    @inject("Redlock") private lockFactory: Redlock
  ) {}

    readonly FeeSymbol = "ETH";
    readonly FeeDecimals = 18;
    readonly FEE_ESTIMATE_MULTIPLIER = BigInt(4);

    public async GetBatchTransactionAsync(request: GetBatchTransactionRequest): Promise<TransactionResponse> {
        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
            .getOne();

        if (!network) {
            throw new Error(`Network ${request.NetworkName} not found`);
        }

        let transaction: TransactionResponse = null;

        for (const transactionId of request.TransactionHashes) {
            transaction = await this.GetTransactionByHashAsync(network, transactionId);
        }

        if (!transaction) {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    public async GetTransactionAsync(request: GetTransactionRequest): Promise<TransactionResponse> {
        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:name)", { name: request.NetworkName })
            .getOne();

        if (!network) {
            throw new Error(`Network ${request.NetworkName} not found`);
        }

        const transaction = await this.GetTransactionByHashAsync(network, request.TransactionHash);

        if (!transaction) {
            throw new TransactionNotComfirmedException(`Transaction ${request.TransactionHash} not found`);
        }

        return transaction;
    }

    private async GetTransactionByHashAsync(network: Networks, transactionHash: string): Promise<TransactionResponse> {

        const node = network.nodes.find((n) => n.type === NodeType.Primary);
        if (!node) {
            throw new Error(
                `Node with type ${NodeType.Primary} is not configured in ${network.name}`
            );
        }

        const provider = new RpcProvider({ nodeUrl: node.url });

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

        const feeInWei = confrimedTransaction.actual_fee;

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

    public async GetLastConfirmedBlockNumberAsync(request: BaseRequest): Promise<BlockNumberResponse> {
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

        const provider = new RpcProvider({
            nodeUrl: node.url,
        });

        const lastBlockNumber = await provider.getBlockNumber();

        const blockData = await provider.getBlockWithTxHashes(lastBlockNumber) as BLOCK_WITH_TX_HASHES;

        return {
            BlockNumber: lastBlockNumber,
            BlockHash: blockData.block_hash,
        };
    }

    public async GetEventsAsync(request: EventRequest): Promise<HTLCBlockEventResponse> {

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

        const solverAddress = network.managedAccounts.find(m => m.type === AccountType.LP)?.address;

        const htlcAddress = network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)?.address;

        const tokens = await this.dbContext.Tokens
            .createQueryBuilder("currencies")
            .leftJoinAndSelect("currencies.network", "n")
            .getMany();

        const provider = new RpcProvider({
            nodeUrl: node.url
        });

        return TrackBlockEventsAsync(
            network.name,
            provider,
            tokens,
            solverAddress,
            request.FromBlock,
            request.ToBlock,
            htlcAddress,
        );
    }

    public async GetNextNonceAsync(request: NextNonceRequest): Promise<string> {
        return null;
    }

    public async PublishTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string> {
        let result: string;

        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:nName)", { nName: request.networkName })
            .getOneOrFail();

        const node = network.nodes.find(n => n.type === NodeType.Primary);

        if (!node) {
            throw new Error(`Primary node not found for network ${request.networkName}`);
        }

        const privateKey = await new PrivateKeyRepository().getAsync(request.fromAddress);

        const provider = new RpcProvider({
            nodeUrl: node.url
        });

        const account = new Account(provider, request.fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(request.callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: request.fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: request.fee.FixedFeeData.FeeInWei,
            chainId: network.chainId as constants.StarknetChainId,
            nonce: request.nonce
        };

        const calcualtedTxHash = await hash.calculateInvokeTransactionHash(args);

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

    public async BuildTransactionAsync(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse> {
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
                case TransactionType.Approve:
                    return CreateApproveCallData(network, request.Args);
                case TransactionType.Transfer:
                    return CreateTransferCallData(network, request.Args);
                default:
                    throw new Error(`Unknown function name ${request.TransactionType}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async GetBalanceAsync(request: BalanceRequest): Promise<BalanceResponse> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${request.NetworkName}`);
            }

            const token = network.tokens.find(t => t.asset === request.Asset);

            if (!token) {
                throw new Error(`Token not found for network ${request.NetworkName} and asset ${request.Asset}`);
            }

            const provider = new RpcProvider({
                nodeUrl: node.url
            });

            const erc20 = new Contract(erc20Json as Abi, token.tokenContract, provider);
            const balanceResult = await erc20.balanceOf(request.Address);
            const balanceInWei = BigNumber.from(uint256.uint256ToBN(balanceResult.balance as any).toString());

            let result: BalanceResponse = {
                Decimals: this.FeeDecimals,
                AmountInWei: balanceInWei.toString(),
                Amount: Number(utils.formatUnits(balanceInWei.toString(), this.FeeDecimals)),
            }

            return result;
        }
        catch (error) {
            throw error;
        }
    }

    public async SimulateTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string> {

        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:nName)", { nName: request.networkName })
            .getOneOrFail();

        const node = network.nodes.find(n => n.type === NodeType.Primary);

        if (!node) {
            throw new Error(`Primary node not found for network ${request.networkName}`);
        }

        const privateKey = await new PrivateKeyRepository().getAsync(request.fromAddress);

        const provider = new RpcProvider({
            nodeUrl: node.url
        });

        const account = new Account(provider, request.fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(request.callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: request.fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: request.fee.FixedFeeData.FeeInWei,
            chainId: network.chainId as constants.StarknetChainId,
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

    public async EstimateFeeAsync(feeRequest: EstimateFeeRequest): Promise<Fee> {
        try {
            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: feeRequest.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${feeRequest.NetworkName}`);
            }

            const privateKey = await new PrivateKeyRepository().getAsync(feeRequest.FromAddress);

            const provider = new RpcProvider({
                nodeUrl: node.url
            });

            const account = new Account(provider, feeRequest.FromAddress, privateKey, '1');

            var transferCall: Call = JSON.parse(feeRequest.CallData);

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
                Decimals: this.FeeDecimals
            }

            const balanceResponse = await this.GetBalanceAsync({
                Address: feeRequest.FromAddress,
                NetworkName: feeRequest.NetworkName,
                Asset: this.FeeSymbol
            });

            var amount = feeInWei;
            amount = amount.add(utils.parseUnits(feeRequest.Amount.toString(), this.FeeDecimals));

            if (BigNumber.from(balanceResponse.AmountInWei) < amount) {
                throw new Error(`Insufficient balance for fee. Balance: ${balanceResponse}, Fee: ${amount}`);
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

    public async ValidateAddLockSignatureAsync(request: AddLockSignatureRequest): Promise<boolean> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${request.NetworkName}`);
            }

            const provider = new RpcProvider({
                nodeUrl: node.url
            });

            const addlockData: TypedData = {
                domain: {
                    name: 'Train',
                    version: shortString.encodeShortString("v1"),
                    chainId: network.chainId,
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
                    Id: cairo.uint256(request.Id),
                    hashlock: cairo.uint256(request.Hashlock),
                    timelock: cairo.uint256(request.Timelock),
                },
            }

            return await provider.verifyMessageInStarknet(addlockData, request.SignatureArray, request.SignerAddress);
        }
        catch (error) {
            throw error;
        }
    }

    public async GetSpenderAllowanceAsync(request: AllowanceRequest): Promise<number> {

        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .leftJoinAndSelect("network.contracts", "c")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: request.NetworkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${request.NetworkName}`);
            }

            const token = network.tokens.find(t => t.asset === request.Asset);

            if (!token) {
                throw new Error(`Token not found for network ${request.NetworkName} and asset ${request.Asset}`);
            }

            const spenderAddress = !token.tokenContract
                ? network.contracts.find(c => c.type === ContractType.HTLCNativeContractAddress)?.address
                : network.contracts.find(c => c.type === ContractType.HTLCTokenContractAddress)?.address;

            const provider = new RpcProvider({ nodeUrl: node.url });
            const { abi: tokenAbi } = await provider.getClassAt(token.tokenContract);
            const ercContract = new Contract(tokenAbi, token.tokenContract, provider);
            var response: BigInt = await ercContract.allowance(request.OwnerAddress, spenderAddress);

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
  