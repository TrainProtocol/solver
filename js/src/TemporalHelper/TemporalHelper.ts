import { ActivityOptions } from "@temporalio/workflow";
import { v4 as uuidv4 } from 'uuid';
import { TransactionType } from "../Blockchain/Blockchain.Abstraction/Models/TransacitonModels/TransactionType";

export function defaultActivityOptions(taskQueue?: string): ActivityOptions {
  return {
    scheduleToCloseTimeout: '2 days',
    startToCloseTimeout: '1 hour',
    taskQueue,
  };
}

export function buildProcessorId(networkName: string, type: TransactionType, uniqueId: string = uuidv4()): string {
  return `${networkName}-${type}-${uniqueId}`;
}