import { proxyActivities, executeChild, uuid4 } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { InvalidTimelockException } from '../../Blockchain.Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '../../Blockchain.Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../Blockchain.Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../Blockchain.Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../Blockchain.Abstraction/Exceptions/TransactionFailedException';
import { buildProcessorId, decodeJson } from '../../Blockchain.Abstraction/Extensions/StringExtensions';
import { AllowanceRequest } from '../../Blockchain.Abstraction/Models/AllowanceRequest';
import { TransactionResponse } from '../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionRequest';
import { HTLCLockTransactionPrepareRequest } from '../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest';
import { TransferPrepareRequest } from '../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest';
import { TransactionType } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType';

const defaultActivities = proxyActivities<IStarknetBlockchainActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
});

const nonRetryableActivities = proxyActivities<IStarknetBlockchainActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
    retry: {
        nonRetryableErrorTypes: [
            InvalidTimelockException.name,
            HashlockAlreadySetException.name,
            HTLCAlreadyExistsException.name,
            AlreadyClaimedExceptions.name,
            TransactionFailedException.name,
        ],
    },
});

export async function StarknetTransactionProcessor(
    request: TransactionRequest,
    context: TransactionExecutionContext
): Promise<TransactionResponse> {

    if (request.type === TransactionType.HTLCLock) {
        await checkAllowance(request);
    }

    const preparedTransaction = await defaultActivities.BuildTransaction({
        network: request.network,
        prepareArgs: request.prepareArgs,
        type: request.type,
        fromAddress: request.fromAddress,
        swapId: request.swapId,
    });
    try {

        if (!context.fee) {
            context.fee = await nonRetryableActivities.EstimateFee({
                network: request.network,
                toAddress: preparedTransaction.toAddress,
                amount: preparedTransaction.amount,
                fromAddress: request.fromAddress,
                asset: preparedTransaction.asset!,
                callData: preparedTransaction.data,
            });
        }

        if (!context.nonce) {
            context.nonce = await defaultActivities.getNextNonce({
                network: request.network,
                address: request.fromAddress,
            });
        }

        const simulationTxId = await nonRetryableActivities.SimulateTransaction({
            network: request.network,
            fromAddress: request.fromAddress,
            nonce: context.nonce,
            callData: preparedTransaction.data,
            fee: context.fee,
        });

        context.publishedTransactionIds.push(simulationTxId);

        const txId = await nonRetryableActivities.PublishTransaction({
            network: request.network,
            fromAddress: request.fromAddress,
            nonce: context.nonce,
            callData: preparedTransaction.data,
            fee: context.fee,
        });

        context.publishedTransactionIds.push(txId);

        const confirmed = await nonRetryableActivities.GetBatchTransaction({
            network: request.network,
            TransactionHashes: context.publishedTransactionIds,
        });

        confirmed.asset = preparedTransaction.callDataAsset;
        confirmed.amount = preparedTransaction.callDataAmount.toString();

        return confirmed;

    }
    catch (error) {
        if (error instanceof InvalidTimelockException && context.nonce) {

            const transferArgs: TransferPrepareRequest = {
                amount: 0,
                asset: context.fee!.Asset,
                toAddress: request.fromAddress,
            };

            const processorId = buildProcessorId(uuid4(), request.network.name, TransactionType.Transfer);

            const transferRequest: TransactionRequest = {
                prepareArgs: JSON.stringify(transferArgs),
                type: TransactionType.Transfer,
                fromAddress: request.fromAddress,
                network: request.network,
                swapId: request.swapId,
            };

            const childContext: TransactionExecutionContext = {
                attempts: 0,
                nonce: context.nonce,
                fee: null,
                publishedTransactionIds: [],
            };

            await executeChild(StarknetTransactionProcessor,
                {
                    args: [transferRequest, childContext],
                    workflowId: processorId,
                });
        }

        throw error;
    }
}

export async function checkAllowance(context: TransactionRequest): Promise<void> {
    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(context.prepareArgs);

    const allowance = await defaultActivities.GetSpenderAllowance(
        {
            network: context.network,
            ownerAddress: context.fromAddress!,
            asset: lockRequest.sourceAsset,
        } as AllowanceRequest);

    if (lockRequest.amount > allowance) {

        const approveRequest: TransactionRequest = {
            prepareArgs: JSON.stringify({
                Amount: 1000000000,
                Asset: lockRequest.sourceAsset,

            }),
            type: TransactionType.Approve,
            fromAddress: context.fromAddress,
            network: context.network,
            swapId: context.swapId,
        };

        const childContext: TransactionExecutionContext = {
            attempts: 0,
            nonce: null,
            fee: null,
            publishedTransactionIds: [],
        };

        const processorId = buildProcessorId(uuid4(), context.network.name, context.type);
        
        await executeChild(StarknetTransactionProcessor,
            {
                args: [approveRequest, childContext],
                workflowId: processorId,
            });
    }
}