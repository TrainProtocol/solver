import { IBlockchainActivities } from "../../../CoreAbstraction/IBlockchainActivities";
import { AllowanceRequest } from "../../../CoreAbstraction/Models/AllowanceRequest";
import { GetBatchTransactionRequest } from "../../../CoreAbstraction/Models/GetBatchTransactionRequest";
import { TransactionResponse } from "../../../CoreAbstraction/Models/TransacitonModels/TransactionResponse";
import { StarknetPublishTransactionRequest } from "../Models/StarknetPublishTransactionRequest ";

export interface IStarknetBlockchainActivities extends IBlockchainActivities {
    SimulateTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetSpenderAllowanceAsync(request: AllowanceRequest): Promise<number>;
  
    PublishTransactionAsync(request: StarknetPublishTransactionRequest): Promise<string>;
  
    GetBatchTransactionAsync(request: GetBatchTransactionRequest): Promise<TransactionResponse>;
  }