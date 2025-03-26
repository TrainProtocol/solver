export interface AddLockSignatureModel {
    NetworkName: string;
    R?: string;
    S?: string;
    V?: string;
    Signature?: string;
    SignatureArray?: string[];
    Timelock: number;
  }
  
  export interface AddLockSignatureRequest extends AddLockSignatureModel {
    Id: string;
    Hashlock: string;
    SignerAddress: string;
    Asset: string;
  }