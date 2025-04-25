import { proxyActivities, executeChild } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { InvalidTimelockException } from '../../../../Common/Abstraction/Exceptions/InvalidTimelockException';
import { HashlockAlreadySetException } from '@blockchain/common/Abstraction/Exceptions/HashlockAlreadySetException';
import { AlreadyClaimedExceptions } from '../../../../Common/Abstraction/Exceptions/AlreadyClaimedExceptions';
import { HTLCAlreadyExistsException } from '../../../../Common/Abstraction/Exceptions/HTLCAlreadyExistsException';
import { TransactionFailedException } from '../../../../Common/Abstraction/Exceptions/TransactionFailedException';
import { decodeJson } from '../../../../Common/Abstraction/Extensions/StringExtensions';
import { buildProcessorId } from '../../../../Common/src/TemporalHelper/TemporalHelper';
import { AllowanceRequest } from '../../../../Common/Abstraction/Models/AllowanceRequest';
import { TransactionResponse } from '../../../../Common/Abstraction/Models/ReceiptModels/TransactionResponse';
import { TransactionExecutionContext } from '../../../../Common/Abstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionRequest } from '../../../../Common/Abstraction/Models/TransacitonModels/TransactionRequest';
import { HTLCLockTransactionPrepareRequest } from '../../../../Common/Abstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest';
import { TransferPrepareRequest } from '../../../../Common/Abstraction/Models/TransactionBuilderModels/TransferPrepareRequest';
import { TransactionType } from '../../../../Common/Abstraction/Models/TransacitonModels/TransactionType';

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
                    workflowId: buildProcessorId(request.NetworkName, TransactionType.Transfer),
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

        const childContext: TransactionExecutionContext = {};

        await executeChild(StarknetTransactionProcessor,
            {
                args: [approveRequest, childContext],
                workflowId: buildProcessorId(context.NetworkName, context.Type),
            });
    }
}