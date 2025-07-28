export interface TransferPrepareRequest {
    toAddress: string;
    asset: string;
    amount: number;
    memo?: string;
    fromAddress?: string;
  }