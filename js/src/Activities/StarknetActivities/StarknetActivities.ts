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


export class StarknetActivities {
    constructor(private dbContext: SolverContext) { }

    readonly FeeSymbol = "ETH";
    readonly FeeDecimals = 18;
    readonly FEE_ESTIMATE_MULTIPLIER = BigInt(4);

    public async StarknetPublishTransactionAsync(
        fromAddress: string,
        networkName: string,
        nonce?: string,
        callData?: string,
        fee?: Fee): Promise<string> {
        let result: string;

        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
            .getOneOrFail();

        const node = network.nodes.find(n => n.type === NodeType.Primary);

        if (!node) {
            throw new Error(`Primary node not found for network ${networkName}`);
        }

        const privateKey = await new PrivateKeyRepository().getAsync(fromAddress);

        const provider = new RpcProvider({
            nodeUrl: node.url
        });

        const account = new Account(provider, fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: fee.FixedFeeData.FeeInWei,
            chainId: network.chainId as constants.StarknetChainId,
            nonce: nonce
        };

        const calcualtedTxHash = await hash.calculateInvokeTransactionHash(args);

        try {

            const executeResponse = await account.execute(
                [transferCall],
                undefined,
                {
                    maxFee: fee.FixedFeeData.FeeInWei,
                    nonce: nonce
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

    public async StarknetBuildTransactionAsync(
        networkName: string, transactionType: TransactionType, args: string): Promise<TransferBuilderResponse> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .leftJoinAndSelect("network.contracts", "c")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
                .getOneOrFail();

            switch (transactionType) {
                case TransactionType.HTLCLock:
                    return StarknetTransactionBuilder.CreateLockCallData(network, args);
                case TransactionType.HTLCRedeem:
                    return StarknetTransactionBuilder.CreateRedeemCallData(network, args);
                case TransactionType.HTLCRefund:
                    return StarknetTransactionBuilder.CreateRefundCallData(network, args);
                case TransactionType.HTLCAddLockSig:
                    return StarknetTransactionBuilder.CreateAddLockSigCallData(network, args);
                case TransactionType.Approve:
                    return StarknetTransactionBuilder.CreateApproveCallData(network, args);
                case TransactionType.Transfer:
                    return StarknetTransactionBuilder.CreateTransferCallData(network, args);
                default:
                    throw new Error(`Unknown function name ${transactionType}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async StarknetEnsureSufficientBalanceAsync(networkName: string, address: string, asset: string, amount: number): Promise<void> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${networkName}`);
            }

            const token = network.tokens.find(t => t.asset === asset);

            if (!token) {
                throw new Error(`Token not found for network ${networkName} and asset ${asset}`);
            }

            const provider = new RpcProvider({
                nodeUrl: node.url
            });

            const erc20 = new Contract(erc20Json as Abi, token.tokenContract, provider);

            const balanceResult = await erc20.balanceOf(address);
            const balanceInWei = BigNumber.from(uint256.uint256ToBN(balanceResult.balance as any).toString()).toString();
            const balance = Number(utils.formatUnits(balanceInWei, token.decimals));

            if (balance <= amount) {
                throw new Error(`Insufficient balance on ${address}. Balance is less than ${amount}`);
            }
        }
        catch (error) {
            throw error;
        }
    }

    public async StarknetSimulateTransactionAsync(
        fromAddress: string,
        networkName: string,
        nonce?: string,
        callData?: string,
        fee?: Fee): Promise<string> {

        const network = await this.dbContext.Networks
            .createQueryBuilder("network")
            .leftJoinAndSelect("network.nodes", "n")
            .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
            .getOneOrFail();

        const node = network.nodes.find(n => n.type === NodeType.Primary);

        if (!node) {
            throw new Error(`Primary node not found for network ${networkName}`);
        }

        const privateKey = await new PrivateKeyRepository().getAsync(fromAddress);

        const provider = new RpcProvider({
            nodeUrl: node.url
        });

        const account = new Account(provider, fromAddress, privateKey, '1');

        var transferCall: Call = JSON.parse(callData);

        const compiledCallData = transaction.getExecuteCalldata([transferCall], await account.getCairoVersion());

        const args: CalcV2InvokeTxHashArgs = {
            senderAddress: fromAddress,
            version: ETransactionVersion2.V1,
            compiledCalldata: compiledCallData,
            maxFee: fee.FixedFeeData.FeeInWei,
            chainId: network.chainId as constants.StarknetChainId,
            nonce: nonce
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
                    nonce: nonce
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

    public async StarknetEstimateFeeAsync(networkName: string, feeRequest: GetFeesRequest): Promise<Fee> {
        try {
            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${networkName}`);
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

    public async StarknetValidateAddLockSignatureAsync( networkName: string, request: AddLockSignatureRequest): Promise<boolean> {
        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${networkName}`);
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

    public async StarknetGetSpenderAllowanceAsync(networkName: string ,ownerAddress: string ,spenderAddress: string ,asset: string ): Promise<number> {

        try {

            const network = await this.dbContext.Networks
                .createQueryBuilder("network")
                .leftJoinAndSelect("network.nodes", "n")
                .leftJoinAndSelect("network.tokens", "t")
                .where("UPPER(network.name) = UPPER(:nName)", { nName: networkName })
                .getOneOrFail();

            const node = network.nodes.find(n => n.type === NodeType.Primary);

            if (!node) {
                throw new Error(`Primary node not found for network ${networkName}`);
            }

            const token = network.tokens.find(t => t.asset === asset);

            if (!token) {
                throw new Error(`Token not found for network ${networkName} and asset ${asset}`);
            }

            const provider = new RpcProvider({ nodeUrl: node.url });
            const { abi: tokenAbi } = await provider.getClassAt(token.tokenContract);
            const ercContract = new Contract(tokenAbi, token.tokenContract, provider);
            var response: BigInt = await ercContract.allowance(ownerAddress, spenderAddress);

            return Number(utils.formatUnits(response.toString(), token.decimals))
        }
        catch (error) {
            throw error;
        }
    }
}