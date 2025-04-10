import { IBlockchainActivities } from "../../../CoreAbstraction/IBlockchainActivities";
import { AllowanceRequest } from "../../../CoreAbstraction/Models/AllowanceRequest";
import { EstimateFeeRequest } from "../../../CoreAbstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../../CoreAbstraction/Models/FeesModels/Fee";
import { GetBatchTransactionRequest } from "../../../CoreAbstraction/Models/GetBatchTransactionRequest";
import { NextNonceRequest } from "../../../CoreAbstraction/Models/NextNonceRequest";
import { GetTransactionRequest } from "../../../CoreAbstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../../CoreAbstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../../CoreAbstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../../CoreAbstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { StarknetPublishTransactionRequest } from "../Models/StarknetPublishTransactionRequest ";

export interface IStarknetBlockchainActivities extends IBlockchainActivities {
    SimulateTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetSpenderAllowanceAsync(request: AllowanceRequest): Promise<number>;
  
    PublishTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetBatchTransactionAsync(request: GetBatchTransactionRequest): Promise<TransactionResponse>;
    
    EstimateFeeAsync(request: EstimateFeeRequest): Promise<Fee>;
    
    GetNextNonceAsync(request: NextNonceRequest): Promise<string>;
  
    BuildTransactionAsync(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;
  
    GetTransactionAsync(request: GetTransactionRequest): Promise<TransactionResponse>;
  }