import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import * as activities from '../Activities';
import { AddLayerswapAzureAppConfiguration } from '../Config/appConfig';

async function run() {  
  dotenv.config();
  await AddLayerswapAzureAppConfiguration();

  try
  {
    const connection = await NativeConnection.connect({
      address: process.env.TemporalAtomic_ServerHost,
    });

    const worker = await Worker.create({
      namespace: 'atomic',
      taskQueue: 'atomicJs',
      activities,
      connection,
    });

    await worker.run();
  }
  catch (e)
  {
    console.error(`Error starting worker: ${e.message}`);
    return;
  }
}

run().catch((err) => {
  console.error('Error starting worker:', err);
  process.exit(1);
});
