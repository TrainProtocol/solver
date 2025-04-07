import { Provider } from "starknet";
import { Tokens } from "../../../../Data/Entities/Tokens";
import { HTLCBlockEventResponse } from "../../../../CoreAbstraction/Models/EventModels/HTLCBlockEventResposne";

export class StarknetEventTracker {

    public static TrackBlockEventsAsync(
        networkName: string,
        provider: Provider,
        tokens: Tokens[],
        solverAddress: string,
        fromBlock: number,
        toBlock: number,
        htlcContractAddress: string) : Promise<HTLCBlockEventResponse>
    {
        return null;
    }

}