import starknetWorker from './Blockchain/Blockchain.Starknet/Worker/StarknetWorker';
import fuelWorker from './Blockchain/Blockchain.Fuel/Worker/FuelWorker';

const network = process.env.TrainSolver__WorkerNetwork;

// Run the corresponding worker based on the network name
switch (network) {
    case 'starknet':
        starknetWorker(network);
        break;
    case 'fuel':
        fuelWorker(network);
        break;
    default:
        console.error(`Unknown network: ${network}. Supported networks are: starknet.`);
        process.exit(1);
}