
import { IBlockchainActivities } from "../../../../Common/Abstraction/Interfaces/IBlockchainActivities";
import { AllowanceRequest } from "../../../../Common/Abstraction/Models/AllowanceRequest";
import { EstimateFeeRequest } from "../../../../Common/Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../../../Common/Abstraction/Models/FeesModels/Fee";
import { GetBatchTransactionRequest } from "../../../../Common/Abstraction/Models/GetBatchTransactionRequest";
import { NextNonceRequest } from "../../../../Common/Abstraction/Models/NextNonceRequest";
import { GetTransactionRequest } from "../../../../Common/Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../../../Common/Abstraction/Models/ReceiptModels/TransactionResponse";
import { AddLockSignatureRequest } from "../../../../Common/Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { TransactionBuilderRequest } from "../../../../Common/Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../../../Common/Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
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

  ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean>
}