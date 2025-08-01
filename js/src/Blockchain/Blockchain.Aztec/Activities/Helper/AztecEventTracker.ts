
import { AztecAddress, createAztecNodeClient } from '@aztec/aztec.js';
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { formatAddress } from "../AztecBlockchainActivities";

export default async function TrackBlockEventsAsync(
    network: DetailedNetworkDto,
    fromBlock: number,
    toBlock: number,
    solverAddresses: string[],
): Promise<HTLCBlockEventResponse> {

    const tokenCommittedSelector = 2050960156;
    const tokenLockAddedSelector = 3251955602;

    const htlcContractAddress = network.htlcTokenContractAddress;
    const response: HTLCBlockEventResponse = {
        htlcCommitEventMessages: [],
        htlcLockEventMessages: [],
    };

    try {
        if (fromBlock == toBlock) fromBlock = fromBlock - 3;

        const provider = createAztecNodeClient(network.nodes[0].url);

        const blockResponse = await provider.getPublicLogs({
            contractAddress: AztecAddress.fromString(htlcContractAddress),
            fromBlock: fromBlock,
            toBlock: toBlock
        });

        if (!blockResponse) {
            throw new Error(`No blocks found between ${fromBlock} and ${toBlock}`);
        }

        for (const { log: { fields }, id } of blockResponse.logs) {

            const blockNumber = await provider.getBlock(id.blockNumber)
            const txId = blockNumber.body.txEffects[id.txIndex].txHash.toString();

            let selector = fields[0].toNumber();
            let commitId = fields[1].toString();
            let timelock = fields[4].toNumber()

            if (selector == tokenCommittedSelector) {

                const amount = fields[2].toNumber();
                const sourceAsset = fields[3].toString();
                const sourceReceiver = fields[5].toString();
                const destNetwork = readLowest30BytesAsString(fields[6].toString());
                const destAsset = readLowest30BytesAsString(fields[7].toString())

                const receiverAddress = solverAddresses.find(
                    x => formatAddress(x) === formatAddress(sourceReceiver));

                const destAddress = [8, 9, 10]
                    .map(i => readLowest30BytesAsString(fields[i].toString()))
                    .join('');

                const commitMsg: HTLCCommitEventMessage = {
                    txId: txId,
                    commitId: commitId,
                    amount: amount.toString(),
                    receiverAddress: receiverAddress,
                    sourceNetwork: network.name,
                    senderAddress: '0',
                    sourceAsset: sourceAsset,
                    destinationAddress: destAddress,
                    destinationNetwork: destNetwork,
                    destinationAsset: destAsset,
                    timeLock: timelock,
                };

                response.htlcCommitEventMessages.push(commitMsg);

            }
            else if (selector == tokenLockAddedSelector) {

                const hashlockfirst = '0x' + fields[2].toString().slice(-32);
                const hashlocksecond = fields[3].toString().slice(-32);

                const hashLock = hashlockfirst + hashlocksecond;
                const lockMsg: HTLCLockEventMessage = {
                    txId: txId,
                    commitId: commitId,
                    hashLock: hashLock,
                    timeLock: timelock,
                };

                response.htlcLockEventMessages.push(lockMsg);
            }
            return response;
        }
    }
    catch (error) {

        throw error;
    }
}

function readLowest30BytesAsString(hex: string): string {
    if (hex.startsWith('0x')) {
        hex = hex.slice(2);
    }

    const byteLength = hex.length / 2;

    const start = Math.max(0, byteLength - 30) * 2;
    const lowest30Hex = hex.slice(start);

    const bytes = new Uint8Array(lowest30Hex.length / 2);
    for (let i = 0; i < lowest30Hex.length; i += 2) {
        bytes[i / 2] = parseInt(lowest30Hex.slice(i, i + 2), 16);
    }

    let decoded = new TextDecoder().decode(bytes).replace(/\x00/g, '').trim();

    return decoded;
}