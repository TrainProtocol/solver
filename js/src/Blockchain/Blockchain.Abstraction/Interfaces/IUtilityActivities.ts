import { BlockRangeModel } from "../Models/BlockRangeModel";

export interface IUtilityActivities {
  GenerateBlockRanges(start: number, end: number, chunkSize: number): Promise<BlockRangeModel[]>;
}
