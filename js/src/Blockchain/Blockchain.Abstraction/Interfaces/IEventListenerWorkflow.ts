import { NetworkType } from '../../../Data/Entities/Networks';

export interface IEventListenerWorkflow {
  GetLastScannedBlock(): Promise<number | undefined>;

  ProcessedTransactionHashes(): Promise<Set<string>>;

  RunAsync(
    networkName: string,
    networkType: NetworkType,
    blockBatchSize: number,
    waitInterval: string,
    lastProcessedBlockNumber?: number
  ): Promise<void>;
}
  