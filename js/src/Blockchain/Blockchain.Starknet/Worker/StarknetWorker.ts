import { Worker, NativeConnection } from '@temporalio/worker';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { extractActivities as ExtractActivities } from '../../../TemporalHelper/ActivityParser';
import { container } from 'tsyringe';
import { AddCoreServices } from '../../Blockchain.Abstraction/Infrastructure/AddCoreServices';

export default async function run( taskQueue: string): Promise<void> {

  try {

    await AddCoreServices();

    const blockchainActivities = ExtractActivities(container.resolve(StarknetBlockchainActivities));

    const activities = {
      ...blockchainActivities,
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
    console.error(`Error starting worker: ${e.message}`);
    return;
  }
}