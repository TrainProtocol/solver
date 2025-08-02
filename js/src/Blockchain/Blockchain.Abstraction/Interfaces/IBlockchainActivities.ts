import { BalanceRequest } from "../Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../Models/BaseRequest";
import { BlockNumberResponse } from "../Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../Models/EventRequest";
import { AddLockSignatureRequest } from "../Models/TransactionBuilderModels/AddLockSignatureRequest";

export interface IBlockchainActivities {
    getBalance(request: BalanceRequest): Promise<BalanceResponse>;  
  
    getLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse>;  
    
    validateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean>;
    
    getEvents(request: EventRequest): Promise<HTLCBlockEventResponse>;
  }