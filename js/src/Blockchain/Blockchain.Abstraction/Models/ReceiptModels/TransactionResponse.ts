import { TransactionStatus } from "../TransacitonModels/TransactionStatus";

export interface TransactionResponse {
    amount?: string;
    asset?: string;
    decimals: number;
    networkName: string;
    transactionHash: string;
    confirmations: number;
    timestamp: Date;
    feeAmount: string;
    feeAsset: string;
    feeDecimals: number;
    status: TransactionStatus;
  }