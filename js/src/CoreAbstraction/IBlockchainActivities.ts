import { BalanceRequest } from "./Models/BalanceRequestModels/BalanceRequest";
import { AddLockSignatureRequest } from "../lib/Model/TransactionBuilderModels/AddLockSignatureRequest";
import { BalanceResponse } from "./Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "./Models/BaseRequest";
import { BlockNumberResponse } from "./Models/BlockNumberResponse";
import { EventRequest } from "./Models/EventRequest";

export interface IBlockchainActivities {
    GetBalanceAsync(request: BalanceRequest): Promise<BalanceResponse>;
  
    GetSpenderAddressAsync(request: SpenderAddressRequest): Promise<string>;
  
    GetLastConfirmedBlockNumberAsync(request: BaseRequest): Promise<BlockNumberResponse>;
  
    EstimateFeeAsync(request: EstimateFeeRequest): Promise<Fee>;
  
    ValidateAddLockSignatureAsync(request: AddLockSignatureRequest): Promise<boolean>;
  
    GetEventsAsync(request: EventRequest): Promise<HTLCBlockEventResponse>;
  
    GetReservedNonceAsync(request: ReservedNonceRequest): Promise<string>;
  
    GetNextNonceAsync(request: NextNonceRequest): Promise<string>;
  
    BuildTransactionAsync(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;
  
    GetTransactionAsync(request: GetTransactionRequest): Promise<TransactionResponse>;
  }