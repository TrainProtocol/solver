export interface HTLCRedeemTransactionPrepareRequest {
    commitId: string;
    secret: string;
    asset: string;
    destinationAddress?: string;
    senderAddress?: string;
  }