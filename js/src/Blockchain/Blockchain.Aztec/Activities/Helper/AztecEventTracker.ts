
import abi from '../ABIs/ERC20.json' with { type: 'json' };
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { TokenCommittedEvent } from "../Models/FuelTokenCommitedEvents";
import { TokenLockedEvent } from "../Models/FuelTokenLockedEvent";
import { Tokens } from "../../../../Data/Entities/Tokens";
import { AztecAddress, createAztecNodeClient } from '@aztec/aztec.js';
import { formatUnits } from 'ethers/lib/utils.js';

export default async function TrackBlockEventsAsync(
    networkName: string,
    nodeUrl: string,
    fromBlock: number,
    toBlock: number,
    htlcContractAddress: string,
    solverAddress: string,
    tokens: Tokens[],
): Promise<HTLCBlockEventResponse> {

    const tokenCommittedSelector = 2050960156n;
    const tokenLockAddedSelector = 3251955602n;

    const response: HTLCBlockEventResponse = {
        HTLCCommitEventMessages: [],
        HTLCLockEventMessages: [],
    };

    try {
        if (fromBlock == toBlock) fromBlock = fromBlock - 3;

        const provider = createAztecNodeClient(nodeUrl);

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

            let selector = fields[0].toBigInt();
            let commitId = fields[1].toString();
            let timelock = fields[4].toBigInt()

            if (selector == tokenCommittedSelector) {

                const amount = fields[2].toBigInt();
                const sourceAsset = fields[3].toString();
                const sourceReceiver = fields[5].toString();
                const destNetwork = readLowest30BytesAsString(fields[6].toString());
                const destAsset = readLowest30BytesAsString(fields[7].toString())

                const sourceToken = tokens.find(t => t.asset === sourceAsset.trim() && t.network.name === networkName);
                const destToken = tokens.find(t => t.asset === destAsset && t.network.name === destNetwork);

                const destAddress = [8, 9, 10]
                    .map(i => readLowest30BytesAsString(fields[i].toString()))
                    .join('');

                const commitMsg: HTLCCommitEventMessage = {
                    TxId: txId,
                    Id: commitId,
                    Amount: Number(formatUnits(amount, sourceToken.decimals)),
                    AmountInWei: amount.toString(),
                    ReceiverAddress: sourceReceiver,
                    SourceNetwork: networkName,
                    SenderAddress: '0',
                    SourceAsset: sourceAsset,
                    DestinationAddress: destAddress,
                    DestinationNetwork: destNetwork,
                    DestinationAsset: destAsset,
                    TimeLock: timelock.toString(),
                    DestinationNetworkType: destToken.network.type,
                    SourceNetworkType: sourceToken.network.type,
                };

                response.HTLCCommitEventMessages.push(commitMsg);

            }
            else if (selector == tokenLockAddedSelector) {

                const hashlockfirst = '0x' + fields[2].toString().slice(-32);
                const hashlocksecond = fields[3].toString().slice(-32);

                const hashLock = hashlockfirst + hashlocksecond;
                const lockMsg: HTLCLockEventMessage = {
                    TxId: txId,
                    Id: commitId,
                    HashLock: hashLock,
                    TimeLock: timelock.toString(),
                };

                response.HTLCLockEventMessages.push(lockMsg);
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