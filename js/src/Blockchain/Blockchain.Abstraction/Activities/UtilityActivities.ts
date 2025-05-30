import { BlockRangeModel } from "../Models/BlockRangeModel";
import { TransactionType } from "../Models/TransacitonModels/TransactionType";
import { v4 as uuidv4 } from 'uuid';

export function GenerateBlockRanges(start: number, end: number, chunkSize: number): BlockRangeModel[] {
    if (chunkSize <= 0) {
        throw new Error('Chunk size must be greater than 0');
    }

    const result: BlockRangeModel[] = [];
    let currentStart = start;

    while (currentStart <= end) {
        const currentEnd = Math.min(currentStart + chunkSize - 1, end);
        result.push({ From: currentStart, To: currentEnd });
        currentStart = currentEnd + 1;
    }

    return result;
}

export function BuildProcessorId(networkName: string, type: TransactionType): string {
   return `${networkName}-${type}-${uuidv4()}`;
}
