import starknetWorker from './Blockchain/Blockchain.Starknet/Worker/StarknetWorker';
import fuelWorker from './Blockchain/Blockchain.Fuel/Worker/FuelWorker';

const network = process.env.TrainSolver__NetworkType;

// Run the corresponding worker based on the network name
switch (network) {
    case 'Starknet':
        starknetWorker(network);
        break;
    case 'Fuel':
        fuelWorker(network);
        break;
    default:
        console.error(`Unknown network: ${network}. Supported networks are: Starknet, Fuel.`);
        process.exit(1);
}