import { TransactionContext } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionContext";
import { TransactionResponse } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionResponse";
import { TransactionType } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionType";
import { HTLCLockTransactionPrepareRequest } from "../../../lib/Model/TransactionBuilderModels/HTLCLockTransactionPrepareRequest";
import { proxyActivities } from '@temporalio/workflow';

export async function StarknetTransactionProcessor(
    context: TransactionContext
  ): Promise<TransactionResponse> {

    const starknet = proxyActivities<StarknetActivitiesInterface>(activityOptions);

    
    if (context.Type === TransactionType.HTLCLock) {
        await checkAllowanceAsync(context);
      }
    return { /* something */ } as TransactionResponse;
  }

async function checkAllowanceAsync(context:type) {
      // Step 1) Parse the lock request from context.PrepareArgs
  const lockRequest = JSON.parse(context.PrepareArgs) as HTLCLockTransactionPrepareRequest;

  // Step 2) Execute the activity to get the spender address
  // Replace "StarknetActivities.getSpenderAddressAsync" with your actual activity name
  const spenderAddress = await Workflow.executeActivity<string>(
    "StarknetActivities.getSpenderAddressAsync",
    [
      {
        Asset: lockRequest.SourceAsset,
        NetworkName: lockRequest.SourceNetwork,
      },
    ],
    {
      scheduleToCloseTimeout: "2 days",
      startToCloseTimeout: "1 hour",
      // optionally define a 'taskQueue', or rely on default
    }
  );

  const allowance = await Workflow.executeActivity<number>(
    "StarknetActivities.StarknetGetSpenderAllowance",
    [
      {
        NetworkName: lockRequest.SourceNetwork,
        OwnerAddress: context.FromAddress,
        SpenderAddress: spenderAddress,
        Asset: lockRequest.SourceAsset,
      },
    ],
    {
      scheduleToCloseTimeout: "2 days",
      startToCloseTimeout: "1 hour",
    }
  );

  if (lockRequest.Amount > allowance) {

    const approveContext: TransactionContext = {
      ...context,
      PrepareArgs: JSON.stringify({
        SpenderAddress: spenderAddress,
        Amount: 1000000000, // or whatever
        Asset: lockRequest.SourceAsset,
      }),
      Type: TransactionType.Approve,
      PublishedTransactionIds: [],
    };

    // We'll assume your transaction processor is a function named "starknetTransactionProcessor"
    // If you haven't defined it yet, we can add that later.
    await Workflow.startChild("starknetTransactionProcessor", [approveContext], {
      workflowId: buildProcessorId(lockRequest.SourceNetwork, TransactionType.Approve),
    });
  }
}