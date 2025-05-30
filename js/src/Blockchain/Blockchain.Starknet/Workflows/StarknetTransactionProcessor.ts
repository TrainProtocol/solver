import { proxyActivities, executeChild } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { InvalidTimelockException } from '../../Blockchain.Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '../../Blockchain.Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../Blockchain.Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../Blockchain.Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../Blockchain.Abstraction/Exceptions/TransactionFailedException';
import { decodeJson } from '../../Blockchain.Abstraction/Extensions/StringExtensions';
import { AllowanceRequest } from '../../Blockchain.Abstraction/Models/AllowanceRequest';
import { TransactionResponse } from '../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionRequest';
import { HTLCLockTransactionPrepareRequest } from '../../Blockchain.Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest';
import { TransferPrepareRequest } from '../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest';
import { TransactionType } from '../../Blockchain.Abstraction/Models/TransacitonModels/TransactionType';
import { IUtilityActivities } from '../../Blockchain.Abstraction/Interfaces/IUtilityActivities';

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

const utilityActivities = proxyActivities<IUtilityActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
});

export async function StarknetTransactionProcessor(
    request: TransactionRequest,
    context: TransactionExecutionContext
): Promise<TransactionResponse> {

    if (request.Type === TransactionType.HTLCLock) {
        await checkAllowance(request);
    }

    const preparedTransaction = await defaultActivities.BuildTransaction({
        NetworkName: request.NetworkName,
        Args: request.PrepareArgs,
        TransactionType: request.Type,
    });
    try {

        if (!context.Fee) {
            context.Fee = await nonRetryableActivities.EstimateFee({
                NetworkName: request.NetworkName,
                ToAddress: preparedTransaction.ToAddress,
                Amount: preparedTransaction.Amount,
                FromAddress: request.FromAddress,
                Asset: preparedTransaction.Asset!,
                CallData: preparedTransaction.Data,
            });
        }

        if (!context.Nonce) {
            context.Nonce = await defaultActivities.GetNextNonce({
                NetworkName: request.NetworkName,
                Address: request.FromAddress,
            });
        }

        const simulationTxId = await nonRetryableActivities.SimulateTransaction({
            NetworkName: request.NetworkName,
            FromAddress: request.FromAddress,
            Nonce: context.Nonce,
            CallData: preparedTransaction.Data,
            Fee: context.Fee,
        });

        context.PublishedTransactionIds.push(simulationTxId);

        const txId = await nonRetryableActivities.PublishTransaction({
            NetworkName: request.NetworkName,
            FromAddress: request.FromAddress,
            Nonce: context.Nonce,
            CallData: preparedTransaction.Data,
            Fee: context.Fee,
        });

        context.PublishedTransactionIds.push(txId);

        const confirmed = await nonRetryableActivities.GetBatchTransaction({
            NetworkName: request.NetworkName,
            TransactionHashes: context.PublishedTransactionIds,
        });

        confirmed.Asset = preparedTransaction.CallDataAsset;
        confirmed.Amount = preparedTransaction.CallDataAmount;

        return confirmed;

    }
    catch (error) {
        if (error instanceof InvalidTimelockException && context.Nonce) {
            const transferArgs: TransferPrepareRequest = {
                Amount: 0,
                Asset: context.Fee!.Asset,
                ToAddress: request.FromAddress,
            };

            const processorId = await utilityActivities.BuildProcessorId(request.NetworkName, TransactionType.Transfer);

            await executeChild(StarknetTransactionProcessor,
                {
                    args: [
                        {
                            NetworkName: request.NetworkName,
                            FromAddress: request.FromAddress,
                            NetworkType: request.NetworkType,
                            PrepareArgs: JSON.stringify(transferArgs),
                            Type: TransactionType.Transfer,
                            SwapId: request.SwapId,
                        },
                        { Nonce: context.Nonce }
                    ],
                    workflowId: processorId,
                });
        }

        throw error;
    }
}

export async function checkAllowance(context: TransactionRequest): Promise<void> {
    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

    const allowance = await defaultActivities.GetSpenderAllowance(
        {
            NetworkName: lockRequest.SourceNetwork,
            OwnerAddress: context.FromAddress!,
            Asset: lockRequest.SourceAsset,
        } as AllowanceRequest);

    if (lockRequest.Amount > allowance) {
        const approveRequest: TransactionRequest = {
            PrepareArgs: JSON.stringify({
                Amount: 1000000000,
                Asset: lockRequest.SourceAsset,

            }),
            Type: TransactionType.Approve,
            FromAddress: context.FromAddress,
            NetworkName: lockRequest.SourceNetwork,
            NetworkType: context.NetworkType,
            SwapId: context.SwapId,
        };

        const childContext: TransactionExecutionContext = {
            Attempts: 0,
            Nonce: null,
            Fee: null,
            PublishedTransactionIds: [],
        };
        const processorId = await utilityActivities.BuildProcessorId(context.NetworkName, context.Type);

        await executeChild(StarknetTransactionProcessor,
            {
                args: [approveRequest, childContext],
                workflowId: processorId,
            });
    }
}