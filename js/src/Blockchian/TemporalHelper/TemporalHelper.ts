import { ActivityOptions } from "@temporalio/workflow";
import { TransactionType } from "../../CoreAbstraction/Models/TransacitonModels/TransactionType";
import { v4 as uuidv4 } from 'uuid';

export const defaultActivityOptions: ActivityOptions = {
  scheduleToCloseTimeout: "2 days",
  startToCloseTimeout: "1 hour",
};

export function buildProcessorId(networkName: string, type: TransactionType, uniqueId: string = uuidv4()): string {
  return `${networkName}-${type}-${uniqueId}`;
}