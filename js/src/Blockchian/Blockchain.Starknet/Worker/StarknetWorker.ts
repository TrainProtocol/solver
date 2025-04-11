import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { extractActivities as ExtractActivities } from '../../TemporalHelper/ActivityParser';
import { NetworkType } from '../../../Data/Entities/Networks';
import { container } from 'tsyringe';
import { AddCoreServices } from '../../Blockchain.Abstraction/Infrastructure/AddCoreServices';

async function run() {
  dotenv.config();

  try {

    await AddCoreServices();

    const activities = ExtractActivities(container.resolve(StarknetBlockchainActivities));

    const connection = await NativeConnection.connect({
      address: process.env.TrainSolver__TemporalServerHost,
    });

    const worker = await Worker.create({
      namespace: 'atomic',
      taskQueue: NetworkType.Starknet.toString(),
      workflowsPath: require.resolve('../Workflows/StarknetWorkflow'),
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

run().catch((err) => {
  console.error('Error starting worker:', err);
  process.exit(1);
});
