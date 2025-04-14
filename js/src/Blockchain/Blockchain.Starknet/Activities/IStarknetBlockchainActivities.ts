
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
    SimulateTransaction(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetSpenderAllowance(request: AllowanceRequest): Promise<number>;
  
    PublishTransaction(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetBatchTransaction(request: GetBatchTransactionRequest): Promise<TransactionResponse>;
    
    EstimateFee(request: EstimateFeeRequest): Promise<Fee>;
    
    GetNextNonce(request: NextNonceRequest): Promise<string>;
  
    BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;
  
    GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;
  }