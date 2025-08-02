import { TransactionType } from "../Models/TransacitonModels/TransactionType";

export interface IUtilityActivities {
  BuildProcessorId(networkName: string, type: TransactionType): Promise<string>;
}
