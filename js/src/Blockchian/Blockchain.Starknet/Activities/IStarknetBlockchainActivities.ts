
import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { AllowanceRequest } from "../../Blockchain.Abstraction/Models/AllowanceRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetBatchTransactionRequest } from "../../Blockchain.Abstraction/Models/GetBatchTransactionRequest";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NextNonceRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
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