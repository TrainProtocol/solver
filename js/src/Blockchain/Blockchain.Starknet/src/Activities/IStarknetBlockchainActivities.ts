import { AddLockSignatureRequest, AllowanceRequest, EstimateFeeRequest, Fee, GetBatchTransactionRequest, GetTransactionRequest, IBlockchainActivities, NextNonceRequest, PrepareTransactionResponse, TransactionBuilderRequest, TransactionResponse } from "@blockchain/common";
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