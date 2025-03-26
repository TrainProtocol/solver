import { Abi, Account, cairo, Call, CallData, constants, Contract, hash, RpcProvider, shortString, transaction, TransactionType as StarknetTransactionType, uint256 } from "starknet";
import { SolverContext } from "../../Data/SolverContext";
import { ParseNonces } from "./Helper/ErrorParser";
import { ETransactionVersion2, TypedData, TypedDataRevision } from "starknet-types-07";
import { PrivateKeyRepository } from "../../lib/PrivateKeyRepository";
import { BigNumber, utils } from "ethers";
import erc20Json from './ABIs/ERC20.json';
import { TransferBuilderResponse } from "../../lib/Model/TransactionBuilderModels/TransferBuilderResponse";
import { InvalidTimelockException } from "../../Exceptions/InvalidTimelockException";
import { GetFeesRequest } from "../../lib/Model/GetFeesModels/GetFeesRequest";
import { Fee, FixedFeeData } from "../../lib/Model/GetFeesModels/GetFeesResponse";
import { NodeType } from "../../Data/Entities/Nodes";
import { AddLockSignatureRequest } from "../../lib/Model/TransactionBuilderModels/AddLockSignatureRequest";
import { StarknetTransactionBuilder } from "./Helper/StarknetTransactionBuilder";
import { CalcV2InvokeTxHashArgs } from "../../lib/Model/WithdrawalModels/TransactioCalculationType";
import { TransactionType } from "../../lib/Model/TransactionTypes/TransactionType";
import { StarknetPublishTransactionRequest } from "./Models/StarknetPublishTransactionRequest ";
import { SufficientBalanceRequest } from "../../lib/Model/BalanceRequestModels/SufficientBalanceRequest";
import { AllowanceRequest } from "../../lib/Model/AllowanceModels/AllowanceRequest";
import { TransactionBuilderRequest } from "../../lib/Model/TransactionBuilderModels/TransactionBuilderRequest";


export class StarknetActivities {
    constructor(private dbContext: SolverContext) { }

    readonly FeeSymbol = "ETH";
    readonly FeeDecimals = 18;
    readonly FEE_ESTIMATE_MULTIPLIER = BigInt(4);

    public async StarknetPublishTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string> {
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

    public async StarknetBuildTransactionAsync(request: TransactionBuilderRequest): Promise<TransferBuilderResponse> {
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
                    return StarknetTransactionBuilder.CreateLockCallData(network, request.Args);
                case TransactionType.HTLCRedeem:
                    return StarknetTransactionBuilder.CreateRedeemCallData(network, request.Args);
                case TransactionType.HTLCRefund:
                    return StarknetTransactionBuilder.CreateRefundCallData(network,request.Args);
                case TransactionType.HTLCAddLockSig:
                    return StarknetTransactionBuilder.CreateAddLockSigCallData(network, request.Args);
                case TransactionType.Approve:
                    return StarknetTransactionBuilder.CreateApproveCallData(network, request.Args);
                case TransactionType.Transfer:
                    return StarknetTransactionBuilder.CreateTransferCallData(network, request.Args);
                default:
                    throw new Error(`Unknown function name ${request.TransactionType}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async StarknetEnsureSufficientBalanceAsync(request: SufficientBalanceRequest): Promise<void> {
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
            const balanceInWei = BigNumber.from(uint256.uint256ToBN(balanceResult.balance as any).toString()).toString();
            const balance = Number(utils.formatUnits(balanceInWei, token.decimals));

            if (balance <= request.Amount) {
                throw new Error(`Insufficient balance on ${request.Address}. Balance is less than ${request.Amount}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async StarknetSimulateTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string> {

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

    public async StarknetEstimateFeeAsync(feeRequest: GetFeesRequest): Promise<Fee> {
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

            const feeInWei = (feeEstimateResponse.suggestedMaxFee * this.FEE_ESTIMATE_MULTIPLIER).toString();

            const fixedfeeData: FixedFeeData = {
                FeeInWei: feeInWei,
            };

            let result: Fee =
            {
                Asset: this.FeeSymbol,
                FixedFeeData: fixedfeeData,
                Decimals: this.FeeDecimals
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

    public async StarknetValidateAddLockSignatureAsync(request: AddLockSignatureRequest): Promise<boolean> {
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

    public async StarknetGetSpenderAllowanceAsync(request: AllowanceRequest): Promise<number> {

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

            const provider = new RpcProvider({ nodeUrl: node.url });
            const { abi: tokenAbi } = await provider.getClassAt(token.tokenContract);
            const ercContract = new Contract(tokenAbi, token.tokenContract, provider);
            var response: BigInt = await ercContract.allowance(request.OwnerAddress, request.SpenderAddress);

            return Number(utils.formatUnits(response.toString(), token.decimals))
        }
        catch (error) {
            throw error;
        }
    }
}