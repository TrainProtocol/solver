
import { IBlockchainActivities } from "../../Blockchain.Abstraction/Interfaces/IBlockchainActivities";
import { AllowanceRequest } from "../../Blockchain.Abstraction/Models/AllowanceRequest";
import { EstimateFeeRequest } from "../../Blockchain.Abstraction/Models/FeesModels/EstimateFeeRequest";
import { Fee } from "../../Blockchain.Abstraction/Models/FeesModels/Fee";
import { GetBatchTransactionRequest } from "../../Blockchain.Abstraction/Models/GetBatchTransactionRequest";
import { NextNonceRequest } from "../../Blockchain.Abstraction/Models/NonceModels/NextNonceRequest";
import { GetTransactionRequest } from "../../Blockchain.Abstraction/Models/ReceiptModels/GetTransactionRequest";
import { TransactionResponse } from "../../Blockchain.Abstraction/Models/ReceiptModels/TransactionResponse";
import { AddLockSignatureRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/AddLockSignatureRequest";
import { TransactionBuilderRequest } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransactionBuilderRequest";
import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";
import { ComposeRawTransactionRequest, ComposeRawTransactionResponse } from "../Models/ComposeRawTxModels";
import { EnsureSufficientBalanceRequest } from "../Models/EnsureSufficientBalanceModels";
import { PublishTransactionRequest, SimulateTransactionRequest } from "../Models/TransactionModels";
import { SignTransactionRequest } from "../Models/SignTransactionRequest";

export interface IStarknetBlockchainActivities extends IBlockchainActivities {
  SimulateTransaction(request: SimulateTransactionRequest): Promise<void>;

  GetSpenderAllowance(request: AllowanceRequest): Promise<number>;

  PublishTransaction(request: PublishTransactionRequest): Promise<string>;

  GetBatchTransaction(request: GetBatchTransactionRequest): Promise<TransactionResponse>;

  EstimateFee(request: EstimateFeeRequest): Promise<Fee>;

  GetNextNonce(request: NextNonceRequest): Promise<string>;

  BuildTransaction(request: TransactionBuilderRequest): Promise<PrepareTransactionResponse>;

  GetTransaction(request: GetTransactionRequest): Promise<TransactionResponse>;

  ValidateAddLockSignature(request: AddLockSignatureRequest): Promise<boolean>;

  EnsureSufficientBalance(request: EnsureSufficientBalanceRequest): Promise<void>;

  ComposeRawTransaction(request: ComposeRawTransactionRequest): Promise<ComposeRawTransactionResponse>;

  SignTransaction(request: SignTransactionRequest): Promise<string>;
}