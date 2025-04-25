import { proxyActivities, executeChild } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { TransactionExecutionContext, InvalidTimelockException, HashlockAlreadySetException, AlreadyClaimedExceptions, HTLCAlreadyExistsException, TransactionFailedException, decodeJson, TransactionType, AllowanceRequest, buildProcessorId, HTLCLockTransactionPrepareRequest, TransactionRequest, TransactionResponse, TransferPrepareRequest } from '@blockchain/common'

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