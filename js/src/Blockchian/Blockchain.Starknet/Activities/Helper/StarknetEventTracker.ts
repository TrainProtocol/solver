import { CallData, Contract, hash, num, Provider } from "starknet";
import { Tokens } from "../../../../Data/Entities/Tokens";
import { TokenLockedEvent as TokenLockAddedEvent, TokenLockedEvent } from "../../Models/StarknetTokenLockedEvent";
import { events } from 'starknet';
import trainAbi from '../ABIs/Train.json';
import { TokenCommittedEvent as TokenCommittedEvent } from "../../Models/StarknetTokenCommittedEvent";
import { formatAddress } from "../StarknetBlockchainActivities";
import { formatUnits } from "ethers/lib/utils";
import { HTLCBlockEventResponse, HTLCCommitEventMessage, HTLCLockEventMessage } from "../../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";


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
        if ("TokenCommitted" in parsed) {
            const data = (parsed as unknown as { TokenCommitted: TokenCommittedEvent }).TokenCommitted;

            if (formatAddress(data.SourceReciever) !== formatAddress(solverAddress)) {
                continue;
            }

            const sourceToken = tokens.find(t => t.asset === data.SourceAsset && t.network.name === networkName);
            const destToken = tokens.find(t => t.asset === data.DestinationAsset && t.network.name === data.DestinationNetwork);

            if (!sourceToken || !destToken) continue;

            const commitMsg: HTLCCommitEventMessage = {
                TxId: rawEvents.find(e => e.keys[1] === data.Id.toString())?.transaction_hash ?? "",
                Id: "0x" + data.Id.toString(16),
                Amount: Number(formatUnits(data.AmountInBaseUnits, sourceToken.decimals)),
                AmountInWei: data.AmountInBaseUnits,
                ReceiverAddress: solverAddress,
                SourceNetwork: networkName,
                SenderAddress: formatAddress(data.SenderAddress),
                SourceAsset: data.SourceAsset,
                DestinationAddress: data.DestinationAddress,
                DestinationNetwork: data.DestinationNetwork,
                DestinationAsset: data.DestinationAsset,
                TimeLock: Number(data.Timelock),
                DestinationNetworkType: destToken.network.type,
                SourceNetworkType: sourceToken.network.type,
            };

            response.HTLCCommitEventMessages.push(commitMsg);
        }

        else if ("TokenLockAdded" in parsed) {
            const data = (parsed as unknown as { TokenLockAdded: TokenLockAddedEvent }).TokenLockAdded;

            const lockMsg: HTLCLockEventMessage = {
                TxId: rawEvents.find(e => e.keys[1] === data.Id.toString())?.transaction_hash ?? "",
                Id: "0x" + data.Id.toString(16),
                HashLock: "0x" + data.Hashlock.toString(16),
                TimeLock: Number(data.Timelock),
            };

            response.HTLCLockEventMessages.push(lockMsg);
        }
    }

    return response;
}

export type ContractEvent =
    | Partial<{ TokenCommitted: TokenCommittedEvent }>
    | Partial<{ TokenLockAdded: TokenLockAddedEvent }>;