export interface HTLCRefundTransactionPrepareRequest {
    commitId: string;
    asset: string;
    destinationAddress?: string;
  }