import { TransactionStatus } from "../TransacitonModels/TransactionStatus";

export interface TransactionResponse {
    Amount: number;
    Asset: string;
    NetworkName: string;
    TransactionHash: string;
    Confirmations: number;
    Timestamp: Date;
    FeeAmount: number;
    FeeAsset: string;
    Status: TransactionStatus;
  }