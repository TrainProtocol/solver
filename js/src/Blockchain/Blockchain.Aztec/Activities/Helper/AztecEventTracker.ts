
import { AztecAddress } from '@aztec/aztec.js/addresses';
import { createAztecNodeClient } from '@aztec/aztec.js/node';
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { ensureHexPrefix, removeHexPrefix } from '../../../Blockchain.Abstraction/Extensions/StringExtensions';

export default async function TrackBlockEventsAsync(
    network: DetailedNetworkDto,
    fromBlock: number,
    toBlock: number,
    solverAddresses: string[],
): Promise<HTLCBlockEventResponse> {

    const MinBlockInterval = 3;
    const tokenCommittedSelector = 2050960156;
    const tokenLockAddedSelector = 3251955602;

    const htlcContractAddress = network.htlcTokenContractAddress;
    const response: HTLCBlockEventResponse = {
        htlcCommitEventMessages: [],
        htlcLockEventMessages: [],
    };

    try {
        if (fromBlock == toBlock) {
            fromBlock = fromBlock - MinBlockInterval
        };

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
            let selector = fields[0].toNumber();

            if (selector !== tokenCommittedSelector && selector !== tokenLockAddedSelector) {
                continue;
            }
            const blockNumber = await provider.getBlock(id.blockNumber)
            const txId = blockNumber.body.txEffects[id.txIndex].txHash.toString();

            //In all events, these values order are fixed
            let commitId = fields[1].toString();
            let timelock = fields[4].toNumber()

            if (selector == tokenCommittedSelector) {

                const amount = fields[2].toNumber();
                const sourceReceiver = fields[5].toString();
                const sourceAsset = readLowest30BytesAsString(fields[6].toString());
                const destNetwork = readLowest30BytesAsString(fields[7].toString())
                const destAsset = readLowest30BytesAsString(fields[8].toString());

                const receiverAddress = solverAddresses.find(
                    x => x.toLowerCase() === sourceReceiver.toLowerCase());

                const destAddress = [9, 10, 11]
                    .map(i => readLowest30BytesAsString(fields[i].toString()))
                    .join('');

                const commitMsg: HTLCCommitEventMessage = {
                    txId: txId,
                    commitId: commitId,
                    amount: amount.toString(),
                    receiverAddress: receiverAddress,
                    sourceNetwork: network.name,
                    senderAddress: '',
                    sourceAsset: sourceAsset,
                    destinationAddress: destAddress,
                    destinationNetwork: destNetwork,
                    destinationAsset: destAsset,
                    timeLock: timelock
                };

                response.htlcCommitEventMessages.push(commitMsg);

            }
            else if (selector == tokenLockAddedSelector) {

                const hashLock = hashLockToHexValidated(fields[2].toBigInt(), fields[3].toBigInt())

                const lockMsg: HTLCLockEventMessage = {
                    txId: txId,
                    commitId: commitId,
                    hashLock: hashLock,
                    timeLock: timelock,
                };

                response.htlcLockEventMessages.push(lockMsg);
            }
        }

        return response;
    }
    catch (error) {

        throw error;
    }
}

function readLowest30BytesAsString(hex: string): string {
    removeHexPrefix(hex)

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

function hashLockToHexValidated(high: bigint, low: bigint): string {
    const MAX_128_BIT = (BigInt(1) << BigInt(128)) - BigInt(1);

    if (high < BigInt(0) || high > MAX_128_BIT) {
        throw new Error(`High value out of range. Must be 0 <= high <= ${MAX_128_BIT}`);
    }

    if (low < BigInt(0) || low > MAX_128_BIT) {
        throw new Error(`Low value out of range. Must be 0 <= low <= ${MAX_128_BIT}`);
    }

    // Combine: high << 128 + low
    const combined = (high << BigInt(128)) + low;

    // Convert to hex and pad to 64 characters
    const hexString = combined.toString(16).padStart(64, '0');

    return ensureHexPrefix(hexString);
}