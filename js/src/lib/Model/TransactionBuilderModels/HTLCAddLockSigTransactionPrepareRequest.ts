export interface HTLCAddLockSigTransactionPrepareRequest {
    Id: string;
    Hashlock: string;
    Timelock: number;  
    R?: string;        
    S?: string;        
    V?: string;        
    Signature?: string;
    Asset: string;
    SignatureArray?: string[];
  }