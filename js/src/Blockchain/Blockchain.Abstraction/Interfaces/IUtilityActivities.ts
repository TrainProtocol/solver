import { BlockRangeModel } from "../Models/BlockRangeModel";
import { TransactionType } from "../Models/TransacitonModels/TransactionType";

export interface IUtilityActivities {
  GenerateBlockRanges(start: number, end: number, chunkSize: number): Promise<BlockRangeModel[]>;

  BuildProcessorId(networkName: string, type: TransactionType): Promise<string>;
}
