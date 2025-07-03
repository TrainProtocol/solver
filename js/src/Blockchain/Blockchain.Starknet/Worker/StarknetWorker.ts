import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { extractActivities as ExtractActivities } from '../../../TemporalHelper/ActivityParser';
import { NetworkType } from '../../../Data/Entities/Networks';
import { container } from 'tsyringe';
import { AddCoreServices } from '../../Blockchain.Abstraction/Infrastructure/AddCoreServices';
import * as UtilityActivities from '../../Blockchain.Abstraction/Activities/UtilityActivities';


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

    const namespace = process.env.TrainSolver__TemporalNamespace || 'atomic';
    const worker = await Worker.create({
      namespace: namespace,
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