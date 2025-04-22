import Redis from "ioredis";
import Redlock from "redlock";
import { SolverContext } from "../../../Data/SolverContext";
import { BalanceRequest } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../../Blockchain.Abstraction/Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";
import { BlockNumberResponse } from "../../Blockchain.Abstraction/Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../../Blockchain.Abstraction/Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../../Blockchain.Abstraction/Models/EventRequest";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { ISolanaBlockchainActivities } from "./ISolanaBlockchainActivities";
import { injectable, inject } from "tsyringe";

@injectable()
export class SolanaBlockchainActivities implements ISolanaBlockchainActivities {
    constructor(
        @inject(SolverContext) private dbContext: SolverContext,
        @inject("Redis") private redis: Redis,
        @inject("Redlock") private lockFactory: Redlock
    ) { }
    
    public async GetBalance(request: BalanceRequest): Promise<BalanceResponse> {
        throw new Error("Method not implemented.");
    }
    GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse> {
        throw new Error("Method not implemented.");
    }
    ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean> {
        throw new Error("Method not implemented.");
    }
    GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse> {
        throw new Error("Method not implemented.");
    }
}