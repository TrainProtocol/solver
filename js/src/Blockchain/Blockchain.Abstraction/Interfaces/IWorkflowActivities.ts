import { NetworkType } from '../../../Data/Entities/Networks';
import { HTLCCommitEventMessage } from '../Models/EventModels/HTLCBlockEventResposne';

export interface IWorkflowActivities {
  GetRunningWorkflowIdsAsync(workflowType: string): Promise<string[]>;

  RunEventListeningWorkflowAsync(
    networkName: string,
    networkType: NetworkType,
    blockBatchSize: number,
    waitInterval: string
  ): Promise<void>;

  StartRefundWorkflowAsync(swapId: string): Promise<void>;

  StartSwapWorkflowAsync(signal: HTLCCommitEventMessage): Promise<string>;

  TerminateWorkflowAsync(workflowId: string): Promise<void>;
}
