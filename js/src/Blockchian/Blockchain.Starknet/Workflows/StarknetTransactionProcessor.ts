import { proxyActivities, executeChild } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { TransactionRequest } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionRequest';
import { TransactionExecutionContext } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionResponse } from '../../../CoreAbstraction/Models/ReceiptModels/TransactionResponse';
import { HTLCLockTransactionPrepareRequest } from '../../../CoreAbstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest';
import { decodeJson } from '../../../CoreAbstraction/Extensions/StringExtensions';
import { TransactionType } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionType';
import { AllowanceRequest } from '../../../CoreAbstraction/Models/AllowanceRequest';
import { buildProcessorId } from '../../TemporalHelper/TemporalHelper';
import { InvalidTimelockException } from '../../../CoreAbstraction/Exceptions/InvalidTimelockException';
import { TransferPrepareRequest } from '../../../CoreAbstraction/Models/TransactionBuilderModels/TransferPrepareRequest';
import { HashlockAlreadySetException } from '../../../CoreAbstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../../CoreAbstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../../CoreAbstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../../CoreAbstraction/Exceptions/TransactionFailedException';

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

export async function StarknetTransactionProcessorWorkflow(
    request: TransactionRequest,
    context: TransactionExecutionContext
): Promise<TransactionResponse> {

    if (request.TransactionType === TransactionType.HTLCLock) {
        await checkAllowance(request);
    }

    const preparedTransaction = await defaultActivities.BuildTransactionAsync({
        NetworkName: request.NetworkName,
        Args: request.PrepareArgs,
        TransactionType: request.TransactionType,
    });
    try {

        if (!context.Fee) {
            context.Fee = await nonRetryableActivities.EstimateFeeAsync({
                NetworkName: request.NetworkName,
                ToAddress: preparedTransaction.ToAddress,
                Amount: preparedTransaction.Amount,
                FromAddress: request.FromAddress,
                Asset: preparedTransaction.Asset!,
                CallData: preparedTransaction.Data,
            });
        }

        if (!context.Nonce) {
            context.Nonce = await defaultActivities.GetNextNonceAsync({
                NetworkName: request.NetworkName,
                Address: request.FromAddress,
            });
        }

        const simulationTxId = await nonRetryableActivities.SimulateTransactionAsync({
            NetworkName: request.NetworkName,
            FromAddress: request.FromAddress,
            Nonce: context.Nonce,
            CallData: preparedTransaction.Data,
            Fee: context.Fee,
        });

        context.PublishedTransactionIds.push(simulationTxId);

        const txId = await nonRetryableActivities.PublishTransactionAsync({
            NetworkName: request.NetworkName,
            FromAddress: request.FromAddress,
            Nonce: context.Nonce,
            CallData: preparedTransaction.Data,
            Fee: context.Fee,
        });

        context.PublishedTransactionIds.push(txId);

        const confirmed = await nonRetryableActivities.GetBatchTransactionAsync({
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

            await executeChild(StarknetTransactionProcessorWorkflow,
                {
                    args: [
                        {
                            NetworkName: request.NetworkName,
                            FromAddress: request.FromAddress,
                            NetworkType: request.NetworkType,
                            PrepareArgs: JSON.stringify(transferArgs),
                            TransactionType: TransactionType.Transfer,
                            SwapId: request.SwapId,
                        },
                        { Nonce: context.Nonce }
                    ],
                    workflowId: buildProcessorId(request.NetworkName, TransactionType.Transfer),
                });
        }

        throw error;
    }
}

export async function checkAllowance(context: TransactionRequest): Promise<void> {
    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

    const allowance = await defaultActivities.GetSpenderAllowanceAsync(
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
            TransactionType: TransactionType.Approve,
            FromAddress: context.FromAddress,
            NetworkName: lockRequest.SourceNetwork,
            NetworkType: context.NetworkType,
            SwapId: context.SwapId,
        };

        const childContext: TransactionExecutionContext = {};

        await executeChild(StarknetTransactionProcessorWorkflow,
            {
                args: [approveRequest, childContext],
                workflowId: buildProcessorId(context.NetworkName, context.TransactionType),
            });
    }
}