export interface HTLCAddLockSigTransactionPrepareRequest {
    commitId: string;
    hashlock: string;
    timelock: number;  
    r?: string;        
    s?: string;        
    v?: string;        
    signature?: string;
    asset: string;
    signatureArray?: string[];
  }