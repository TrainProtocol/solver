import {
    proxyActivities,
    defineQuery,
    setHandler,
    sleep,
    continueAsNew,
    getExternalWorkflowHandle,
} from '@temporalio/workflow';
import { IBlockchainActivities } from '../Interfaces/IBlockchainActivities';
import { IUtilityActivities } from '../Interfaces/IUtilityActivities';
import { BlockRangeModel } from '../Models/BlockRangeModel';
import { lockCommitedSignal } from '../Interfaces/ISwapWorkflow';
import { IWorkflowActivities } from '../Interfaces/IWorkflowActivities';
import { TimeSpan } from '../Infrastructure/RedisHelper/TimeSpanConverter';
import { CoreTaskQueue } from '../Constants';

const blockchainActivities = proxyActivities<IBlockchainActivities>({
    startToCloseTimeout: '20s',
    scheduleToCloseTimeout: '20m',
});

const utilityActivities = proxyActivities<IUtilityActivities>({
    startToCloseTimeout: '20s',
    scheduleToCloseTimeout: '20m',
});

const workflowActivities = proxyActivities<IWorkflowActivities>({
    startToCloseTimeout: '20s',
    scheduleToCloseTimeout: '20m',
    taskQueue: CoreTaskQueue
});

let lastProcessedBlockNumber: number | undefined = undefined;
const processedTransactionHashes = new Set<string>();

export const getLastScannedBlock = defineQuery<number | undefined>('getLastScannedBlock');
export const getProcessedTransactionHashes = defineQuery<string[]>('processedTransactionHashes');

export async function EventListenerWorkflow(
    networkName: string,
    networkType: string,
    blockBatchSize: number,
    waitIntervalInSeconds: number,
    initialLastProcessedBlock?: number
): Promise<void> {
    lastProcessedBlockNumber = initialLastProcessedBlock;
    let iteration = 0;

    setHandler(getLastScannedBlock, () => lastProcessedBlockNumber);
    setHandler(getProcessedTransactionHashes, () => Array.from(processedTransactionHashes));

    while (true) {
        if (iteration >= 200) {
            await continueAsNew<typeof EventListenerWorkflow>(
                networkName,
                networkType,
                blockBatchSize,
                waitIntervalInSeconds,
                lastProcessedBlockNumber
            );
        }

        const blockData = await blockchainActivities.GetLastConfirmedBlockNumber({ NetworkName: networkName });

        if (lastProcessedBlockNumber == null) {
            lastProcessedBlockNumber = blockData.BlockNumber - blockBatchSize;
        }

        if (lastProcessedBlockNumber >= blockData.BlockNumber) {
            await sleep(TimeSpan.FromSeconds(waitIntervalInSeconds));
            iteration++;
            continue;
        }

        const blockRanges = await utilityActivities.GenerateBlockRanges(
            lastProcessedBlockNumber - 15,
            blockData.BlockNumber,
            blockBatchSize
        );

        for (const chunk of chunkArray(blockRanges, 4)) {
            await Promise.all(chunk.map(blockRange =>
                processBlockRange(networkName, blockRange)
            ));

            lastProcessedBlockNumber = chunk[chunk.length - 1].To;
        }

        iteration++;
    }
}

async function processBlockRange(
    networkName: string,
    blockRange: BlockRangeModel
): Promise<void> {
    const result = await blockchainActivities.GetEvents({
        NetworkName: networkName,
        FromBlock: blockRange.From,
        ToBlock: blockRange.To,
    });

    for (const commit of result.HTLCCommitEventMessages) {
        if (!processedTransactionHashes.has(commit.TxId)) {
            processedTransactionHashes.add(commit.TxId);
            await workflowActivities.StartSwapWorkflow(commit);
        }
    }

    for (const lock of result.HTLCLockEventMessages) {
        if (!processedTransactionHashes.has(lock.TxId)) {
            processedTransactionHashes.add(lock.TxId);
            try {

                const handle = getExternalWorkflowHandle(lock.Id);
                await handle.signal(lockCommitedSignal, lock);
            }
            catch {
            }
        }
    }
}

function chunkArray<T>(arr: T[], size: number): T[][] {
    const res: T[][] = [];
    for (let i = 0; i < arr.length; i += size) {
        res.push(arr.slice(i, i + size));
    }
    return res;
}
