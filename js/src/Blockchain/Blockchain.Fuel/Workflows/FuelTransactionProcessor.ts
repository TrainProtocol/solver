import { ApplicationFailure, continueAsNew, executeChild, proxyActivities, uuid4 } from '@temporalio/workflow';
import { IFuelBlockchainActivities } from '../Activities/IFuelBlockchainActivities';
import { InvalidTimelockException } from '../../Blockchain.Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '../../Blockchain.Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../Blockchain.Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../Blockchain.Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../Blockchain.Abstraction/Exceptions/TransactionFailedException';
import { TransactionResponse } from '../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionRequest';
import { NetworkType } from '../../Blockchain.Abstraction/Models/Dtos/NetworkDto';
import { buildProcessorId } from '../../Blockchain.Abstraction/Extensions/StringExtensions';

const defaultActivities = proxyActivities<IFuelBlockchainActivities>({
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

     const nextNonce = await defaultActivities.getNextNonce({
            address: request.fromAddress,
            network: request.network
        });

    try {       

        await defaultActivities.checkCurrentNonce(
            {
                address: request.fromAddress,
                network: request.network,
                currentNonce: nextNonce
            }
        )

        const preparedTransaction = await defaultActivities.BuildTransaction({
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
                signerAgentUrl: request.signerAgentUrl,
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

        await defaultActivities.updateCurrentNonce(
            {
                address: request.fromAddress,
                network: request.network,
                currentNonce: nextNonce
            }
        )

        return transactionResponse;

    }
    catch (error) {

         await defaultActivities.updateCurrentNonce(
            {
                address: request.fromAddress,
                network: request.network,
                currentNonce: nextNonce
            }
        )

        if ((error instanceof ApplicationFailure && error.type === 'TransactionFailedException')) {

            const processorId = buildProcessorId(uuid4(), request.network.name, request.type);

            await continueAsNew(FuelTransactionProcessor, {
                args: [request, context],
                workflowId: processorId,
            });
        }
        else {
            throw error;
        }
    }
}