import { NetworkType } from '../../../Data/Entities/Networks';
import { HTLCCommitEventMessage } from '../Models/EventModels/HTLCBlockEventResposne';

export interface IWorkflowActivities {
  GetRunningWorkflowIds(workflowType: string): Promise<string[]>;

  RunEventListeningWorkflow(
    networkName: string,
    networkType: NetworkType,
    blockBatchSize: number,
    waitInterval: string
  ): Promise<void>;

  StartRefundWorkflow(swapId: string): Promise<void>;

  StartSwapWorkflow(signal: HTLCCommitEventMessage): Promise<string>;

  TerminateWorkflow(workflowId: string): Promise<void>;
}
