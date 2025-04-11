export interface TransferPrepareRequest {
    ToAddress: string;
    Asset: string;
    Amount: number;
    Memo?: string;
    FromAddress?: string;
  }