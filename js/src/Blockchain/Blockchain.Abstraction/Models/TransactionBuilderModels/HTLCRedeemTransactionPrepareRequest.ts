export interface HTLCRedeemTransactionPrepareRequest {
    Id: string;
    Secret: string;
    Asset: string;
    DestinationAddress?: string;
    SenderAddress?: string;
  }