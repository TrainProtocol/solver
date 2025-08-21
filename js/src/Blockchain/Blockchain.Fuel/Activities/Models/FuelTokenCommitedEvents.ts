export interface TokenCommittedEvent {
  Id: string;
  dstChain: string;
  dstAddress: string;
  dstAsset: string;
  srcAsset: string;
  amount: string;
  timelock: string;
  srcReceiver: AddressBits;
  sender: AddressBits;
  tokenContract: string;
}

interface AddressBits {
  bits: string;
}
