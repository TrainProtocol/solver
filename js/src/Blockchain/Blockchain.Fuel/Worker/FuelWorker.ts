import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { extractActivities as ExtractActivities } from '../../../TemporalHelper/ActivityParser';
import { container } from 'tsyringe';
import { AddCoreServices } from '../../Blockchain.Abstraction/Infrastructure/AddCoreServices';
import * as UtilityActivities from '../../Blockchain.Abstraction/Activities/UtilityActivities';
import { FuelBlockchainActivities } from '../Activities/FuelBlockchainActivities';

export default async function run( taskQueue: string): Promise<void> {
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
    const namespace = process.env.TrainSolver__TemporalNamespace;

    if (!namespace) {
      throw new Error('TemporalNamespace environment variable is not set.');
    }

    const worker = await Worker.create({
      namespace: namespace,
      taskQueue: taskQueue,
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