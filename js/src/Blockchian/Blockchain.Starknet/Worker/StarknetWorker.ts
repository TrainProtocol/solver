import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { extractActivities as ExtractActivities } from '../../TemporalHelper/ActivityParser';
import { NetworkType } from '../../../Data/Entities/Networks';
import { AddCoreServices } from '../../../CoreAbstraction/Infrastructure/AddCoreServices';
import { container } from 'tsyringe';

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
