import { CallData, hash, num, Provider, events } from "starknet";
import trainAbi from '../ABIs/Train.json';
import { formatAddress } from "../StarknetBlockchainActivities";
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { BigIntToAscii, ensureHexPrefix, toHex } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";
import { DetailedNetworkDto } from "../../../Blockchain.Abstraction/Models/DetailedNetworkDto";
import { TokenCommittedEvent, TokenLockedEvent } from "../../Models/EventModels";


export async function TrackBlockEventsAsync(
    network: DetailedNetworkDto,
    provider: Provider,
    solverAddresses: string[],
    fromBlock: number,
    toBlock: number): Promise<HTLCBlockEventResponse> {

    const response: HTLCBlockEventResponse = {
        htlcCommitEventMessages: [],
        htlcLockEventMessages: [],
    };

    const tokenCommittedSelector = num.toHex(hash.starknetKeccak("TokenCommitted"));
    const tokenLockAddedSelector = num.toHex(hash.starknetKeccak("TokenLockAdded"));
    const htlcContractAddress = network.htlcTokenContractAddress;

    const filter = {
        address: htlcContractAddress,
        from_block: { block_number: fromBlock },
        to_block: { block_number: toBlock },
        keys: [[tokenCommittedSelector, tokenLockAddedSelector]],
        chunk_size: 100
    };

    const eventsPage = await provider.getEvents(filter);

    const rawEvents = eventsPage.events;
    const abiEvents = events.getAbiEvents(trainAbi);
    const abiStructs = CallData.getAbiStruct(trainAbi);
    const abiEnums = CallData.getAbiEnum(trainAbi);
    const parsedEvents = events.parseEvents(rawEvents, abiEvents, abiStructs, abiEnums);

    for (const parsed of parsedEvents) {

        const keys = Object.keys(parsed);
        const eventName = keys[0];
        const eventData = parsed[eventName];

        if (eventName.endsWith("TokenCommitted")) {
            const data = eventData as unknown as TokenCommittedEvent;

            const receiverAddress = solverAddresses.find(
                x => formatAddress(x) === formatAddress(toHex(data.srcReceiver))
            );

            if (!receiverAddress) {
                continue;
            }

            const logEvent = rawEvents.find(x => x.transaction_hash === parsed.transaction_hash);

           const dstAddress = ensureHexPrefix(logEvent.data[8]);

            const commitMsg: HTLCCommitEventMessage = {
                txId: parsed.transaction_hash,
                commitId: toHex(data.Id),
                amount: Number(data.amount).toString(),
                receiverAddress: receiverAddress,
                sourceNetwork: network.name,
                senderAddress: formatAddress(toHex(data.sender)),
                sourceAsset: BigIntToAscii(data.srcAsset),
                destinationAddress: dstAddress,
                destinationNetwork: BigIntToAscii(data.dstChain),
                destinationAsset: BigIntToAscii(data.dstAsset),
                timeLock: Number(data.timelock)
            };

            response.htlcCommitEventMessages.push(commitMsg);
        }
        else if (eventName.endsWith("TokenLockAdded")) {
            const data = eventData as unknown as TokenLockedEvent;

            const lockMsg: HTLCLockEventMessage = {
                txId: parsed.transaction_hash,
                commitId: toHex(data.Id),
                hashLock: toHex(data.hashlock),
                timeLock: Number(data.timelock),
            };

            response.htlcLockEventMessages.push(lockMsg);
        }
    }

    return response;
}

export type ContractEvent =
    | Partial<{ TokenCommitted: TokenCommittedEvent }>
    | Partial<{ TokenLockAdded: TokenLockedEvent }>;