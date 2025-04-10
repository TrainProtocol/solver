import { BalanceRequest } from "./Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "./Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "./Models/BaseRequest";
import { BlockNumberResponse } from "./Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "./Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "./Models/EventRequest";
import { AddLockSignatureRequest } from "./Models/TransactionBuilderModels/AddLockSignatureRequest";

export interface IBlockchainActivities {
    GetBalanceAsync(request: BalanceRequest): Promise<BalanceResponse>;  
  
    GetLastConfirmedBlockNumberAsync(request: BaseRequest): Promise<BlockNumberResponse>;  
    
    ValidateAddLockSignatureAsync(request: AddLockSignatureRequest): Promise<boolean>;
    
    GetEventsAsync(request: EventRequest): Promise<HTLCBlockEventResponse>;
  }