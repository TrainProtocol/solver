import { DetailedNetworkDto } from "../DetailedNetworkDto";

export interface AddLockSignatureModel {
  r?: string;
  s?: string;
  v?: string;
  signature?: string;
  signatureArray?: string[];
  timelock: number;
}

export interface AddLockSignatureRequest extends AddLockSignatureModel {
  commitId: string;
  hashlock: string;
  signerAddress: string;
  asset: string;
  detailedNetworkDto: DetailedNetworkDto;
}