import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { extractActivities as ExtractActivities } from '../../../TemporalHelper/ActivityParser';
import { NetworkType } from '../../../Data/Entities/Networks';
import { container } from 'tsyringe';
import { AddCoreServices } from '../../Blockchain.Abstraction/Infrastructure/AddCoreServices';
import * as UtilityActivities from '../../Blockchain.Abstraction/Activities/UtilityActivities';
import { FuelBlockchainActivities } from '../Activities/FuelBlockchainActivities';

export default async function run() {
  dotenv.config();

  try {

    await AddCoreServices();

    const blockchainActivities = ExtractActivities(container.resolve(FuelBlockchainActivities));

    const activities = {
      ...blockchainActivities,
      ...UtilityActivities,
    };

    const connection = await NativeConnection.connect({
      address: process.env.TrainSolver__TemporalServerHost,
    });

    const worker = await Worker.create({
      namespace: 'atomic',
      taskQueue: NetworkType[NetworkType.Fuel],
      workflowsPath: require.resolve('../Workflows'),
      activities: activities,
      connection,
    });

    await worker.run();
  }
  catch (e) {
    throw new Error(`Exception happened in FuelWorker: ${e.message}`);
    return;
  }
}