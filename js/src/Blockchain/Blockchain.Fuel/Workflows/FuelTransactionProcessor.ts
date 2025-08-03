import { ApplicationFailure, executeChild, proxyActivities } from '@temporalio/workflow';
import { IFuelBlockchainActivities } from '../Activities/IFuelBlockchainActivities';
import { InvalidTimelockException } from '../../Blockchain.Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '../../Blockchain.Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../Blockchain.Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../Blockchain.Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../Blockchain.Abstraction/Exceptions/TransactionFailedException';
import { TransactionResponse } from '../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionRequest';
import { IUtilityActivities } from '../../Blockchain.Abstraction/Interfaces/IUtilityActivities';
import { NetworkType } from '../../Blockchain.Abstraction/Models/Dtos/NetworkDto';

const defaultActivities = proxyActivities<IFuelBlockchainActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
});

const utilityActivities = proxyActivities<IUtilityActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
});

const nonRetryableActivities = proxyActivities<IFuelBlockchainActivities>({
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

export async function FuelTransactionProcessor(
    request: TransactionRequest,
    context: TransactionExecutionContext
): Promise<TransactionResponse> {

    try {

        const preparedTransaction = await defaultActivities.buildTransaction({
            fromAddress: request.fromAddress,
            network: request.network,
            prepareArgs: request.prepareArgs,
            type: request.type,
        });

        const rawTx = await defaultActivities.composeRawTransaction({
            network: request.network,
            fromAddress: request.fromAddress,
            callData: preparedTransaction.data,
            callDataAsset: preparedTransaction.callDataAsset,
            callDataAmount: preparedTransaction.callDataAmount,
        });

        const signedRawData = await defaultActivities.signTransaction(
            {
                networkType: NetworkType[request.network.type],
                signRequest: {
                    unsignedTxn: rawTx,
                    address: request.fromAddress,
                    nodeUrl: request.network.nodes[0].url,
                }
            }
        );

        // sign transaction
        const publishedTransaction = await nonRetryableActivities.publishTransaction({
            network: request.network,
            signedRawData: signedRawData
        });

        const transactionResponse = await defaultActivities.getTransaction({
            network: request.network,
            transactionHash: publishedTransaction,
        });

        transactionResponse.asset = preparedTransaction.callDataAsset;
        transactionResponse.amount = preparedTransaction.callDataAmount.toString();

        return transactionResponse;

    }
    catch (error) {
        if ((error instanceof ApplicationFailure && error.type === 'TransactionFailedException')) {

            const processorId = await utilityActivities.BuildProcessorId(request.network.name, request.type);

            await executeChild(FuelTransactionProcessor,
                {
                    args: [request, context],
                    workflowId: processorId,
                });
        }

        throw new Error(`Failed to process transaction: ${error.message}`);
    }
}