import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { extractActivities as ExtractActivities } from '../../../../Common/src/TemporalHelper/ActivityParser';
import { NetworkType } from '../../../../Common/src/Data/Entities/Networks';
import { container } from 'tsyringe';
import * as UtilityActivities from '../../../../Common/Abstraction/Activities/UtilityActivities';
import { AddCoreServices } from '../../../../Common/Abstraction/Infrastructure/AddCoreServices';

export default async function run() {
  dotenv.config();

  try {

    await AddCoreServices();

    const blockchainActivities = ExtractActivities(container.resolve(StarknetBlockchainActivities));

    const activities = {
      ...blockchainActivities,
      ...UtilityActivities,
    };

    const connection = await NativeConnection.connect({
      address: process.env.TrainSolver__TemporalServerHost,
    });

    const worker = await Worker.create({
      namespace: 'atomic',
      taskQueue: NetworkType[NetworkType.Starknet],
      workflowsPath: require.resolve('../Workflows'),
      activities: activities,
      connection,
    });

    await worker.run();
  }
  catch (e) {
    console.error(`Error starting worker: ${e.message}`);
    return;
  }
}