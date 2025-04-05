import { BalanceRequest } from "./Models/BalanceRequestModels/BalanceRequest";
import { BalanceResponse } from "./Models/BalanceRequestModels/BalanceResponse";
import { BaseRequest } from "./Models/BaseRequest";
import { BlockNumberResponse } from "./Models/BlockNumberResponse";
import { HTLCBlockEventResponse } from "./Models/EventModels/HTLCBlockEventResposne";
import { EventRequest } from "./Models/EventRequest";
import { EstimateFeeRequest } from "./Models/FeesModels/EstimateFeeRequest";
import { Fee } from "./Models/FeesModels/Fee";
import { NextNonceRequest } from "./Models/NextNonceRequest";
import { GetTransactionRequest } from "./Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "./Models/ReceiptModels/TransactionResponse";
import { AddLockSignatureRequest } from "./Models/TransactionBuilderModels/AddLockSignatureRequest";
import { TransactionBuilderRequest } from "./Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "./Models/TransactionBuilderModels/TransferBuilderResponse";

export interface IBlockchainActivities {
    GetBalanceAsync(request: BalanceRequest): Promise<BalanceResponse>;  
  
    GetLastConfirmedBlockNumberAsync(request: BaseRequest): Promise<BlockNumberResponse>;
  
    EstimateFeeAsync(request: EstimateFeeRequest): Promise<Fee>;
  
    ValidateAddLockSignatureAsync(request: AddLockSignatureRequest): Promise<boolean>;
  
    GetEventsAsync(request: EventRequest): Promise<HTLCBlockEventResponse>;
  
    GetNextNonceAsync(request: NextNonceRequest): Promise<string>;
  
    BuildTransactionAsync(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;
  
    GetTransactionAsync(request: GetTransactionRequest): Promise<TransactionResponse>;
  }