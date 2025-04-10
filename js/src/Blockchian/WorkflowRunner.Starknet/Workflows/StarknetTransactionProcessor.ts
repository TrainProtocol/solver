import { proxyActivities, defineSignal, setHandler, executeChild } from '@temporalio/workflow';
import { IStarknetBlockchainActivities } from '../Activities/IStarknetBlockchainActivities';
import { TransactionRequest } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionRequest';
import { TransactionExecutionContext } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionExecutionContext';
import { TransactionResponse } from '../../../CoreAbstraction/Models/ReceiptModels/TransactionResponse';
import { HTLCLockTransactionPrepareRequest } from '../../../CoreAbstraction/Models/TransactionBuilderModels/HTLCLockTransactionPrepareRequest';
import { decodeJson } from '../../../Extensions/StringExtensions';
import { TransactionType } from '../../../CoreAbstraction/Models/TransacitonModels/TransactionType';
import { AllowanceRequest } from '../../../CoreAbstraction/Models/AllowanceRequest';

const activities = proxyActivities<IStarknetBlockchainActivities>({
    startToCloseTimeout: '1 hour',
    scheduleToCloseTimeout: '2 days',
});

export async function StarknetTransactionProcessorWorkflow(
    request: TransactionRequest,
    context: TransactionExecutionContext
): Promise<TransactionResponse> {
    return null!;
}

export async function checkAllowance(context: TransactionRequest): Promise<void> {
    const lockRequest = decodeJson<HTLCLockTransactionPrepareRequest>(context.PrepareArgs);

    if (!lockRequest) {
        throw new Error(`Failed to deserialize HTLCLockTransactionPrepareRequest from: ${context.PrepareArgs}`);
    }

    const allowance = await activities.GetSpenderAllowanceAsync(
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

        await executeChild(StarknetTransactionProcessorWorkflow, {
            args: [approveRequest, childContext],
            workflowId: `approve-${context.NetworkName}-${Date.now()}`, // or generate with your helper
        });
    }
}