import { proxyActivities } from '@temporalio/workflow';
import { IFuelBlockchainActivities } from '../Activities/IFuelBlockchainActivities';
import { InvalidTimelockException } from '../../Blockchain.Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '../../Blockchain.Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../Blockchain.Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../Blockchain.Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../Blockchain.Abstraction/Exceptions/TransactionFailedException';
import { TransactionResponse } from '../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionRequest';

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

    const preparedTransaction = await defaultActivities.BuildTransaction({
        NetworkName: request.NetworkName,
        Args: request.PrepareArgs,
        TransactionType: request.Type,
    });

    if (!context.Fee) {
        context.Fee = await nonRetryableActivities.EstimateFee({
            NetworkName: request.NetworkName,
            ToAddress: preparedTransaction.ToAddress,
            Amount: preparedTransaction.Amount,
            FromAddress: request.FromAddress,
            Asset: preparedTransaction.Asset,
            CallData: preparedTransaction.Data,
        });
    }

    const publishedTransaction = await defaultActivities.PublishTransaction({
        NetworkName: request.NetworkName,
        FromAddress: request.FromAddress,
        CallData: preparedTransaction.Data,
        Fee: context.Fee,
        Amount: preparedTransaction.AmountInWei,
    });

    const transactionResponse = await defaultActivities.GetTransaction({
        NetworkName: request.NetworkName,
        TransactionHash: publishedTransaction,
    });

    transactionResponse.Asset = preparedTransaction.CallDataAsset;
    transactionResponse.Amount = preparedTransaction.CallDataAmount;
    
    return transactionResponse;
}