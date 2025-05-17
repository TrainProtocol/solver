import starknetWorker from './Blockchain/Blockchain.Starknet/Worker/StarknetWorker';
import fuelWorker from './Blockchain/Blockchain.Fuel/Worker/FuelWorker';

// Parse the console argument
const args = process.argv.slice(2);
if (args.length === 0) {
    console.error('Please provide a network name (starknet, fuel).');
    process.exit(1);
}

const network = args[0].toLowerCase();

// Run the corresponding worker based on the network name
switch (network) {
    case 'starknet':
        starknetWorker();
        break;
    case 'fuel':
        fuelWorker();
        break;
    default:
        console.error(`Unknown network: ${network}. Supported networks are: starknet.`);
        process.exit(1);
}