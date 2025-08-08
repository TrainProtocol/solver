import { BalanceRequest } from "../Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "../Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "../Models/BaseRequest";
import { BlockNumberResponse } from "../Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "../Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "../Models/EventRequest";
import { AddLockSignatureRequest } from "../Models/TransactionBuilderModels/AddLockSignatureRequest";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";

export interface IBlockchainActivities {
    BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;

    GetBalance(request: BalanceRequest): Promise<BalanceResponse>;  
  
    GetLastConfirmedBlockNumber(request: BaseRequest): Promise<BlockNumberResponse>;  
    
    ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean>;
    
    GetEvents(request: EventRequest): Promise<HTLCBlockEventResponse>;
  }