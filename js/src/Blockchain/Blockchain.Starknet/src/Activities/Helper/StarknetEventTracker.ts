import { CallData, hash, num, Provider } from "starknet";
import { TokenLockedEvent as TokenLockAddedEvent} from "../../Models/StarknetTokenLockedEvent";
import { events } from 'starknet';
import trainAbi from '../ABIs/Train.json';
import { TokenCommittedEvent as TokenCommittedEvent } from "../../Models/StarknetTokenCommittedEvent";
import { formatAddress as FormatAddress } from "../StarknetBlockchainActivities";
import { formatUnits } from "ethers/lib/utils";
import { BigIntToAscii, HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage, ToHex, Tokens } from "@blockchain/common";


export async function TrackBlockEventsAsync(
    networkName: string,
    provider: Provider,
    tokens: Tokens[],
    solverAddress: string,
    fromBlock: number,
    toBlock: number,
    htlcContractAddress: string): Promise<HTLCBlockEventResponse> {

    const response: HTLCBlockEventResponse = {
        HTLCCommitEventMessages: [],
        HTLCLockEventMessages: [],
    };

    const tokenCommittedSelector = num.toHex(hash.starknetKeccak("TokenCommitted"));
    const tokenLockAddedSelector = num.toHex(hash.starknetKeccak("TokenLockAdded"));

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

            if (FormatAddress(ToHex(data.srcReceiver)) !== FormatAddress(solverAddress)) {
                continue;
            }

            const sourceToken = tokens.find(t => t.asset === BigIntToAscii(data.srcAsset) && t.network.name === networkName);
            const destToken = tokens.find(t => t.asset === BigIntToAscii(data.dstAsset) && t.network.name === BigIntToAscii(data.dstChain));

            if (!sourceToken || !destToken) continue;

            const commitMsg: HTLCCommitEventMessage = {
                TxId: parsed.transaction_hash,
                Id: ToHex(data.Id),
                Amount: Number(formatUnits(data.amount, 18)),
                AmountInWei: data.amount.toString(),
                ReceiverAddress: solverAddress,
                SourceNetwork: networkName,
                SenderAddress: FormatAddress(ToHex(data.sender)),
                SourceAsset: BigIntToAscii(data.srcAsset),
                DestinationAddress: data.dstAddress,
                DestinationNetwork: BigIntToAscii(data.dstChain),
                DestinationAsset: BigIntToAscii(data.dstAsset),
                TimeLock: Number(data.timelock),
                DestinationNetworkType: destToken.network.type,
                SourceNetworkType: sourceToken.network.type,
            };

            response.HTLCCommitEventMessages.push(commitMsg);
        }
        else if (eventName.endsWith("TokenLockAdded")) {
            const data = eventData as unknown as TokenLockAddedEvent;

            const lockMsg: HTLCLockEventMessage = {
                TxId: parsed.transaction_hash,
                Id: ToHex(data.Id),
                HashLock: ToHex(data.hashlock),
                TimeLock: Number(data.timelock),
            };

            response.HTLCLockEventMessages.push(lockMsg);
        }
    }

    return response;
}

export type ContractEvent =
    | Partial<{ TokenCommitted: TokenCommittedEvent }>
    | Partial<{ TokenLockAdded: TokenLockAddedEvent }>;