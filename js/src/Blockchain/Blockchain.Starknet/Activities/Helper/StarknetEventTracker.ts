import { CallData, Contract, hash, num, Provider } from "starknet";
import { Tokens } from "../../../../Data/Entities/Tokens";
import { TokenLockedEvent as TokenLockAddedEvent, TokenLockedEvent } from "../../Models/StarknetTokenLockedEvent";
import { events } from 'starknet';
import trainAbi from '../ABIs/Train.json';
import { TokenCommittedEvent as TokenCommittedEvent } from "../../Models/StarknetTokenCommittedEvent";
import { formatAddress as FormatAddress } from "../StarknetBlockchainActivities";
import { formatUnits } from "ethers/lib/utils";
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { NetworkType } from "../../../../Data/Entities/Networks";
import { BigIntToAscii, ToHex } from "../../../Blockchain.Abstraction/Extensions/StringExtensions";


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

            const commitMsg: HTLCCommitEventMessage = {
                TxId: rawEvents.find(e => e.keys[0])?.transaction_hash,
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
                DestinationNetworkType: NetworkType.Solana,
                SourceNetworkType: NetworkType.Starknet,
            };

            response.HTLCCommitEventMessages.push(commitMsg);
        }

        else if (eventName.endsWith("TokenLockAdded")) {
            const data = (parsed as unknown as { TokenLockAdded: TokenLockAddedEvent }).TokenLockAdded;

            const lockMsg: HTLCLockEventMessage = {
                TxId: rawEvents.find(e => e.keys[0])?.transaction_hash,
                Id: ToHex(data.id),
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