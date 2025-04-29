import { Worker, NativeConnection } from '@temporalio/worker';
import * as dotenv from 'dotenv';
import 'reflect-metadata';
import { StarknetBlockchainActivities } from '../Activities/StarknetBlockchainActivities';
import { container } from 'tsyringe';
import { AddCoreServices, ExtractActivities, NetworkType, UtilityActivities } from '@blockchain/common';

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
    bundlerOptions:
      {
        ignoreModules: [
          'crypto',
          'events',
          'buffer',
          'buffer/',
          'stream',
          'net',
          'tls',
          'dns',
          'http',
          'https',
          'zlib',
          'string_decoder',
          'fs',
          'querystring',
          'path'
        ]
      },
      connection,
    });

    await worker.run();
  }
  catch (e) {
    console.error(`Error starting worker: ${e.message}`);
    return;
  }
}
