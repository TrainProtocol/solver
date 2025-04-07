import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { SolverContext } from '../Data/SolverContext';
import { StarknetBlockchainActivities } from '../Blockchian/WorkflowRunner.Starknet/Activities/StarknetBlockchainActivities';
import { extractActivities } from '../Blockchian/TemporalHelper/ActivityParser';

async function run() {
  dotenv.config();

  try {

    const dbCtx = new SolverContext(process.env.TrainSolver__DatabaseConnectionString);

    const starknetActivities = new StarknetBlockchainActivities(dbCtx);
    
    const activities = extractActivities(starknetActivities);

    const connection = await NativeConnection.connect({
      address: process.env.TrainSolver__TemporalServerHost,
    });

    const worker = await Worker.create({
      namespace: 'atomic',
      taskQueue: 'StarknetJs',
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
